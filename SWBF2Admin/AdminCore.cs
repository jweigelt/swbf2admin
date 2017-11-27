using System;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using SWBF2Admin.Web;
using SWBF2Admin.Database;
using SWBF2Admin.Gameserver;
using SWBF2Admin.Scheduler;
using SWBF2Admin.Rcon;
using SWBF2Admin.Admin;

namespace SWBF2Admin
{
    class AdminCore
    {
        public CoreConfiguration Config { get; }
        public FileHandler Files { get; }
        public WebServer WebAdmin { get; }
        public SQLHandler Database { get; }
        public ServerManager Server { get; }
        public TaskScheduler Scheduler { get; }
        public RconClient Rcon { get; }
        public PlayerHandler Players { get; }

        public AdminCore()
        {
            Logger.Log(LogLevel.Info, Log.CORE_START, Constants.PRODUCT_NAME, Constants.PRODUCT_VERSION, Constants.PRODUCT_AUTHOR);

            Logger.Log(LogLevel.Verbose, Log.CORE_READ_CONFIG);
            Files = new FileHandler();
#if DEBUG
            Config = new CoreConfiguration();
#else
            Config = Files.ReadConfig<CoreConfiguration>();
#endif
            Logger.Log(LogLevel.Info, Log.CORE_READ_CONFIG_OK);

            Database = new SQLHandler();
            Database.SQLEngine = Config.SQLEngine;
            Database.SQLiteFileName = Config.SQLiteFileName;
            Database.MySQLHost = Config.MySQLHostname;
            Database.MySQLDatabase = Config.MySQLDatabaseName;
            Database.MySQLUser = Config.MySQLUsername;
            Database.MySQLPassword = Config.MySQLPassword;

            Rcon = new RconClient();
            Rcon.ServerPassword = Config.RconPassword;
            Rcon.ServerIPEP = Config.GetRconIPEP;
            Rcon.RconDisconnected += new EventHandler(Rcon_Disconnected);
            Rcon.RconChat += new EventHandler(Rcon_Chat);

            Server = new ServerManager(this, Config.ServerPath);
            Server.ServerStarted += new EventHandler(Server_Started);
            Server.ServerStopped += new EventHandler(Server_Stopped);
            Server.ServerCrashed += new EventHandler(Server_Crashed);

            WebAdmin = new WebServer(this, Config.WebAdminPrefix);

            Players = new PlayerHandler(this);
            Scheduler = new TaskScheduler();
        }
        public void Run()
        {
            Scheduler.Start();
            Scheduler.PushTask(new SchedulerTask(_Run));
            Console.ReadLine();
        }
        private void _Run()
        {
            Database.Open();
            if (Config.WebAdminEnable) WebAdmin.Start();

            Server.Open();

            if (Config.ManageServer)
            {
                Logger.Log(LogLevel.Info, "Acting as server manager");
                if (Config.AutoLaunchServer) Server.Start();
            }
            else
            {
                Logger.Log(LogLevel.Info, "Acting as client");
                Scheduler.PushTask(new SchedulerTask(OnServerStart));
            }
        }

        private void Server_Started(object sender, EventArgs e)
        {
            Scheduler.PushTask(new SchedulerTask(OnServerStart));
        }
        private void Server_Crashed(object sender, EventArgs e)
        {
            if (Config.AutoRestartServer)
            {
                Scheduler.PushTask(new SchedulerTask(Server.Start));
            }
        }
        private void Server_Stopped(object sender, EventArgs e)
        {

        }
        private void Rcon_Disconnected(object sender, EventArgs e)
        {
            Scheduler.PushTask(new SchedulerTask(OnServerStop));
        }
        private void Rcon_Chat(object sender, EventArgs e)
        {
            Scheduler.PushTask(new SchedulerTask(() => OnChat((RconChatEventArgs)e)));
        }

        private void OnChat(RconChatEventArgs e)
        {

        }

        private void OnServerStart()
        {
            Logger.Log(LogLevel.Verbose, "Starting runtime management...");
            System.Threading.Thread.Sleep(2000);
            try
            {
                Rcon.ServerPassword = Server.Settings.AdminPw;
                Rcon.Start();
            }
            catch
            {
                Logger.Log(LogLevel.Error, "Aborting...");
                return;
            }

            Scheduler.PushRepeatingTask(new RepeatingSchedulerTask(Players.Update, 10000));

        }
        private void OnServerStop()
        {
            Scheduler.ClearRepeatingTasks();
            Logger.Log(LogLevel.Verbose, "Stopping runtime management...");
            Rcon.Stop();
        }
    }
}