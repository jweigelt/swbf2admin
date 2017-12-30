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

namespace SWBF2Admin
{
    public class AdminCore
    {
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

        private readonly List<ComponentBase> components = new List<ComponentBase>();

        public AdminCore()
        {
            Database = new SQLHandler(this);
            Server = new ServerManager(this);
            WebAdmin = new WebServer(this);
            Rcon = new RconClient(this);
            Players = new PlayerHandler(this);
            Announce = new AnnounceHandler(this);
            Game = new GameHandler(this);
            Commands = new CommandDispatcher(this);
            Mods = new LvlWriter(this);

            components.Add(Database);
            components.Add(Server);
            components.Add(WebAdmin);
            components.Add(Rcon);
            components.Add(Players);
            components.Add(Announce);
            components.Add(Game);
            components.Add(Commands);
            components.Add(Mods);
        }

        public void Run()
        {
            Logger.Log(LogLevel.Info, Log.CORE_START, Util.GetProductName(), Util.GetProductVersion(), Util.GetProductAuthor());
            Logger.Log(LogLevel.Verbose, Log.CORE_READ_CONFIG);
            config = Files.ReadConfig<CoreConfiguration>();
            Logger.Log(LogLevel.Info, Log.CORE_READ_CONFIG_OK);

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
                if (h.UpdateInterval > 0) Scheduler.PushRepeatingTask(h.Update, h.UpdateInterval);
            }

            Scheduler.Start();

            if (Config.ManageServer)
            {
                Logger.Log(LogLevel.Info, "Acting as server manager");
                if (Config.AutoLaunchServer) Scheduler.PushTask(Server.Start);
            }
            else
            {
                Logger.Log(LogLevel.Info, "Acting as client");
                //Scheduler.PushTask(new SchedulerTask(Server_Started));
            }

            string cmd = string.Empty;

            while ((cmd = Console.ReadLine()) != "quit")
            {
                if (cmd == "import maps")
                {
                    Database.ImportMaps(ServerMap.ReadServerMapConfig(this));
                }
            }

            Scheduler.Stop();
            foreach (ComponentBase h in components) h.OnDeInit();
        }

        #region "Events"
        private void Server_Started(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Verbose, "Starting runtime management...");
            foreach (ComponentBase h in components) h.OnServerStart();
        }
        private void Server_Stopped(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Verbose, "Stopping runtime management...");
            foreach (ComponentBase h in components) h.OnServerStop();
        }
        private void Server_Crashed(object sender, EventArgs e)
        {
            Server_Stopped(sender, e);
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