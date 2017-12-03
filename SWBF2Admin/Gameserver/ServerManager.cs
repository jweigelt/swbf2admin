using System;
using System.IO;
using System.Diagnostics;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Gameserver
{
    enum ServerStatus
    {
        Online = 0,
        Offline = 1,
        Starting = 2,
        Stopping = 3
    }

    class ServerManager : ComponentBase
    {
        public event EventHandler ServerCrashed;
        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;

        public string ServerPath { get; set; } = "./server";
        public string ServerArgs { get; set; } = "/win /norender /nosound /autonet dedicated /resolution 640 480";

        private Process serverProcess = null;
        private AdminCore core;
        private ServerStatus status = ServerStatus.Offline;
        public ServerStatus Status { get { return status; } }
        public ServerSettings Settings { get; set; }

        public ServerManager(AdminCore core) : base(core)
        {
            this.core = core;
        }

        public override void Configure(CoreConfiguration config)
        {
            ServerPath = Core.Files.ParseFileName(config.ServerPath);
        }

        public override void OnInit()
        {
            Open();
        }

        private void Open()
        {  
            foreach (Process p in Process.GetProcessesByName("BattlefrontII"))
            {
            
                if (Path.GetFullPath(p.MainModule.FileName).Equals(Path.GetFullPath(ServerPath + "\\BattlefrontII.exe")))
                {
                    Logger.Log(LogLevel.Info, "Found running server process '{0}' ({1}), re-attaching...", p.MainWindowTitle, p.Id.ToString());
                    serverProcess = p;
                    p.EnableRaisingEvents = true;
                    serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                    status = ServerStatus.Online;
                    InvokeEvent(ServerStarted, this, null);
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
                status = ServerStatus.Starting;

                ProcessStartInfo startInfo = new ProcessStartInfo(core.Files.ParseFileName(ServerPath + "/BattlefrontII.exe"), ServerArgs);
                startInfo.WorkingDirectory = core.Files.ParseFileName(ServerPath);
                serverProcess = Process.Start(startInfo);

                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                status = ServerStatus.Online;
                InvokeEvent(ServerStarted, this, new EventArgs());
            }
        }

        public void Stop()
        {
            if (serverProcess != null)
            {
                Logger.Log(LogLevel.Info, "Stopping Server...");
                status = ServerStatus.Stopping;
                serverProcess.Kill();
            }
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            serverProcess = null;

            if (status != ServerStatus.Stopping)
            {
                Logger.Log(LogLevel.Warning, "Server has crashed.");
                status = ServerStatus.Offline;
                InvokeEvent(ServerCrashed, this, new EventArgs());
            }
            else
            {
                Logger.Log(LogLevel.Info, "Server stopped.");
                status = ServerStatus.Offline;
                InvokeEvent(ServerStopped, this, new EventArgs());
            }
        }

    }
}