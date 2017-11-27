using System;
using System.Diagnostics;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Gameserver
{
    enum ServerStatus
    {
        Online = 0,
        Offline = 1,
        Starting = 2,
        Stopping = 3
    }

    class ServerManager
    {
        public event EventHandler ServerCrashed;
        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;

        public string ServerPath { get; set; } = "./server";
        public string ServerArgs { get; set; } = "/win /norender /nosound /autonet dedicated /resolution 640 480";

        private Process serverProcess = null;
        private AdminCore core;
        public ServerInfo Info { get; }
        public ServerSettings Settings { get; set; }

        public ServerManager(AdminCore core, string serverPath)
        {
            this.core = core;
            ServerPath = serverPath;
            Info = new ServerInfo();
            Info.Status = ServerStatus.Offline;
        }

        public void Open()
        {  
            foreach (Process p in Process.GetProcessesByName("BattlefrontII"))
            {
                if (p.MainModule.FileName.ToLower().Equals(ServerPath + "/BattlefrontII.exe")) ;
                {
                    Logger.Log(LogLevel.Info, "Found running server process '{0}' ({1}), re-attaching...", p.MainWindowTitle, p.Id.ToString());
                    serverProcess = p;
                    p.EnableRaisingEvents = true;
                    serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                    Info.Status = ServerStatus.Online;
                    if (ServerStarted != null) ServerStarted.Invoke(this, new EventArgs());
                    break;
                }
            }
            Settings = ServerSettings.FromSettingsFile(core, ServerPath);
        }

        public void Start()
        {
            if (serverProcess == null)
            {
                Logger.Log(LogLevel.Info, "Launching server with args '{0}'", ServerArgs);
                Info.Status = ServerStatus.Starting;

                ProcessStartInfo startInfo = new ProcessStartInfo(core.Files.ParseFileName(ServerPath + "/BattlefrontII.exe"), ServerArgs);
                startInfo.WorkingDirectory = core.Files.ParseFileName(ServerPath);
                serverProcess = Process.Start(startInfo);

                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                Info.Status = ServerStatus.Online;
                if (ServerStarted != null) ServerStarted.Invoke(this, new EventArgs());
            }
        }

        public void Stop()
        {
            if (serverProcess != null)
            {
                Logger.Log(LogLevel.Info, "Stopping Server...");
                Info.Status = ServerStatus.Stopping;
                serverProcess.Kill();
            }
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            //TODO: locks Logger mutex -> sync it
            serverProcess = null;

            if (Info.Status != ServerStatus.Stopping)
            {
                Logger.Log(LogLevel.Warning, "Server has crashed.");
                Info.Status = ServerStatus.Offline;
                if (ServerCrashed != null) ServerCrashed.Invoke(this, new EventArgs());
            }
            else
            {
                Logger.Log(LogLevel.Info, "Server stopped.");
                Info.Status = ServerStatus.Offline;
                if (ServerStopped != null) ServerStopped.Invoke(this, new EventArgs());
            }
        }

    }
}