/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using SWBF2Admin.Web;
using SWBF2Admin.Database;
using SWBF2Admin.Gameserver;
using SWBF2Admin.Scheduler;
using SWBF2Admin.Structures;

using SWBF2Admin.Runtime.Players;
using SWBF2Admin.Runtime.Rcon;
using SWBF2Admin.Runtime.Game;
using SWBF2Admin.Runtime.Announce;
using SWBF2Admin.Runtime.Commands;
using SWBF2Admin.Runtime.ApplyMods;
using SWBF2Admin.Runtime.Watchdog;

using SWBF2Admin.Plugins;

namespace SWBF2Admin
{
    public class AdminCore
    {
        private const string ARG_RESET_WEBUSER = "--reset-webcredentials";

        private CoreConfiguration config;
        public CoreConfiguration Config { get { return config; } }
        public FileHandler Files { get; } = new FileHandler();
        public TaskScheduler Scheduler { get; } = new TaskScheduler();

        public WebServer WebAdmin { get; }
        public SQLHandler Database { get; }
        public ServerManager Server { get; }
        public RconClient Rcon { get; }
        public PlayerHandler Players { get; }
        public AnnounceHandler Announce { get; }
        public GameHandler Game { get; }
        public CommandDispatcher Commands { get; }
        public LvlWriter Mods { get; }
        public PluginManager Plugins { get; }

        private readonly List<ComponentBase> components = new List<ComponentBase>();

        private void SetupWebUser()
        {
            string name, pwd = "", confirmPwd = "";
            Console.WriteLine("A new web user has to be created.");
            Console.Write("Enter Username: ");
            name = Console.ReadLine();

            do
            {
                Console.Write("Enter Password: ");
                pwd = Console.ReadLine();
                Console.Write("Confirm Password: ");
                confirmPwd = Console.ReadLine();

                if (!pwd.Equals(confirmPwd))
                    Console.WriteLine("Passwords don't match.");
            } while (!pwd.Equals(confirmPwd));

            Database.InsertWebUser(new WebUser(name, PBKDF2.HashPassword(Util.Md5(pwd))));
        }

        public AdminCore()
        {
            //TODO: check EnableRuntime before creating these objects to save memory
            Database = new SQLHandler(this);
            Server = new ServerManager(this);
            WebAdmin = new WebServer(this);
            Rcon = new RconClient(this);
            Players = new PlayerHandler(this);
            Announce = new AnnounceHandler(this);
            Game = new GameHandler(this);
            Commands = new CommandDispatcher(this);
            Mods = new LvlWriter(this);
            Plugins = new PluginManager(this);
        }

        public void Run(string[] args)
        {
            Logger.Log(LogLevel.Info, Log.CORE_START, Util.GetProductName(), Util.GetProductVersion(), Util.GetProductAuthor());
            Logger.Log(LogLevel.Verbose, Log.CORE_READ_CONFIG);
            config = Files.ReadConfig<CoreConfiguration>();
            Logger.Log(LogLevel.Info, Log.CORE_READ_CONFIG_OK);
            Logger.MinLevel = config.LogMinimumLevel;
            Logger.LogToFile = config.LogToFile;
            Logger.LogFile = config.LogFileName;

            components.Add(Database);
            components.Add(Server);
            components.Add(WebAdmin);
            components.Add(Plugins);

            if (config.EnableRuntime)
            {
                components.Add(Rcon);
                components.Add(Players);
                components.Add(Announce);
                components.Add(Game);
                components.Add(Commands);
                components.Add(Mods);

                if (Config.EnableEmptyRestart)
                {
                    components.Add(new EmptyRestart(this));
                }
            }

            Scheduler.TickDelay = Config.TickDelay;

            Rcon.Disconnected += new EventHandler(Rcon_Disconnected);
            Rcon.ChatInput += new EventHandler(Rcon_Chat);

            Server.ServerStarted += new EventHandler(Server_Started);
            Server.ServerStopped += new EventHandler(Server_Stopped);
            Server.ServerCrashed += new EventHandler(Server_Crashed);

            Announce.Broadcast += new EventHandler(Announce_Broadcast);

            foreach (ComponentBase h in components)
            {
                h.Configure(Config);
                h.OnInit();
                if (h.UpdateInterval > 0) Scheduler.PushRepeatingTask(h.Task);
            }

            if (!Database.WebUserExists())
            {
                SetupWebUser();
            }
            else
            {
                foreach (string s in args)
                {
                    if (s.Equals(ARG_RESET_WEBUSER))
                    {
                        Logger.Log(LogLevel.Info, "Resetting web users...");
                        Database.TruncateWebUsers();
                        SetupWebUser();
                    }
                }
            }

            Scheduler.Start();
            if (Config.AutoLaunchServer) Scheduler.PushTask(Server.Start);

            string cmd = string.Empty;

            while ((cmd = Console.ReadLine()) != "quit")
            {
                if (cmd == "import maps")
                {
                    Database.ImportMaps(ServerMap.ReadServerMapConfig(this));
                }
                else if (Commands.IsConsoleCommand(cmd))
                {
                    //using the scheduler to execute so we dont have to worry about concurrency
                    Scheduler.PushTask(() => { Commands.HandleConsoleCommand(cmd); });
                }
            }

            Scheduler.Stop();
            foreach (ComponentBase h in components) h.OnDeInit();
        }


        #region "Events"
        private void Server_Started(object sender, EventArgs e)
        {

            Logger.Log(LogLevel.Verbose, "Starting runtime management...");
            Scheduler.PushDelayedTask(() =>
            {
                try
                {
                    foreach (ComponentBase h in components) h.OnServerStart(e);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "Failed to start runtime management ({0})", ex.Message);
                }
            }, config.RuntimeStartDelay);
        }

        private void Server_Stopped(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Verbose, "Stopping runtime management...");
            foreach (ComponentBase h in components) h.OnServerStop();
            if (e != null)
            {
                var se = (StopEventArgs)e;
                if (se.Reason == ServerStopReason.STOP_RESTART)
                {
                    Logger.Log(LogLevel.Verbose, "Restarting server...");
                    Server.Start();
                }
           }
        }

        private void Server_Crashed(object sender, EventArgs e)
        {
            Server_Stopped(sender, null);
            if (Config.AutoRestartServer)
            {
                Logger.Log(LogLevel.Info, "Automatic restart is enabled. Restarting server...");
                Server.Start();
            }
        }

        private void Rcon_Disconnected(object sender, EventArgs e)
        {

        }

        private void Rcon_Chat(object sender, EventArgs e)
        {

        }

        private void Announce_Broadcast(object sender, EventArgs e)
        {
            Rcon.Say(((AnnounceEventArgs)e).Announce.ParseMessage(this));
        }
        #endregion
    }
}