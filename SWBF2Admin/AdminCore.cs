using System;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using SWBF2Admin.Web;
using SWBF2Admin.Database;
using SWBF2Admin.Gameserver;
using SWBF2Admin.Scheduler;

using SWBF2Admin.Runtime.Rcon;
using SWBF2Admin.Runtime;

namespace SWBF2Admin
{
    class AdminCore
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

        private List<ComponentBase> Components { get; }

        public AdminCore()
        {
            Database = new SQLHandler(this);
            Server = new ServerManager(this);
            WebAdmin = new WebServer(this);
            Rcon = new RconClient(this);
            Players = new PlayerHandler(this);

            Components = new List<ComponentBase>();
            Components.Add(Database);
            Components.Add(Server);
            Components.Add(WebAdmin);
            Components.Add(Rcon);
            Components.Add(Players);
        }
        public void Run()
        {
            Logger.Log(LogLevel.Info, Log.CORE_START, Constants.PRODUCT_NAME, Constants.PRODUCT_VERSION, Constants.PRODUCT_AUTHOR);
            Logger.Log(LogLevel.Verbose, Log.CORE_READ_CONFIG);
            config = Files.ReadConfig<CoreConfiguration>();
            Logger.Log(LogLevel.Info, Log.CORE_READ_CONFIG_OK);

            Scheduler.TickDelay = Config.TickDelay;

            Rcon.Disconnected += new EventHandler(Rcon_Disconnected);
            Rcon.ChatInput += new EventHandler(Rcon_Chat);

            Server.ServerStarted += new EventHandler(Server_Started);
            Server.ServerStopped += new EventHandler(Server_Stopped);
            Server.ServerCrashed += new EventHandler(Server_Crashed);

            foreach (ComponentBase h in Components)
            {
                h.Configure(Config);
                if (h.UpdateInterval > 0) Scheduler.PushRepeatingTask(h.OnUpdate, h.UpdateInterval);
                h.OnInit();
            }

            Scheduler.Start();

            if (Config.ManageServer)
            {
                Logger.Log(LogLevel.Info, "Acting as server manager");
                if (Config.AutoLaunchServer) Server.Start();
            }
            else
            {
                Logger.Log(LogLevel.Info, "Acting as client");
                //Scheduler.PushTask(new SchedulerTask(OnServerStart));
            }

            Console.ReadLine();

            Scheduler.Stop();
            foreach (ComponentBase h in Components) h.OnDeInit();
        }

        #region "Events"
        private void Server_Started(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Verbose, "Starting runtime management...");
            foreach (ComponentBase h in Components) h.OnServerStart();
        }
        private void Server_Stopped(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Verbose, "Stopping runtime management...");
            foreach (ComponentBase h in Components) h.OnServerStop();

        }
        private void Server_Crashed(object sender, EventArgs e)
        {
            Server_Stopped(sender, e);
            if (Config.AutoRestartServer)
            {
                Logger.Log(LogLevel.Info, "Automatic restart is enabled. Restart server...");
                Server.Start();
            }
        }

        private void Rcon_Disconnected(object sender, EventArgs e)
        {

        }
        private void Rcon_Chat(object sender, EventArgs e)
        {

        }
        #endregion
    }
}