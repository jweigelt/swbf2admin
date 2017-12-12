using System;
using System.IO;
using System.Diagnostics;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Gameserver
{
    public enum ServerStatus
    {
        Online = 0,
        Offline = 1,
        Starting = 2,
        Stopping = 3
    }

    ///<summary>class handling the gameserver-process</summary>
    public class ServerManager : ComponentBase
    {
        ///<summary>Called if the server-process exits unexpectedly</summary>
        public event EventHandler ServerCrashed;
        ///<summary>Called after the server-process was launched</summary>
        public event EventHandler ServerStarted;
        ///<summary>Called after the server-process exits planned</summary>
        public event EventHandler ServerStopped;

        ///<summary>Relative or absolute path to server installation</summary>
        public string ServerPath { get; set; } = "./server";
    
        ///<summary>Command-line args for starting the gameserver</summary>
        public string ServerArgs { get; set; } = "/win /norender /nosound /autonet dedicated /resolution 640 480";

        private Process serverProcess = null;
        private AdminCore core;
        private ServerStatus status = ServerStatus.Offline;

        ///<summary>Current gameserver status</summary>
        public ServerStatus Status { get { return status; } }
        ///<summary>Settings used by the gameserver</summary>
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

        ///<summary>Checks if the server-process is already running and re-attaches if required</summary>
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

        ///<summary>Launches the gameserver</summary>
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

        /// <summary>Stops the gameserver</summary>
        public void Stop()
        {
            if (serverProcess != null)
            {
                Logger.Log(LogLevel.Info, "Stopping Server...");
                status = ServerStatus.Stopping;
                serverProcess.Kill();
            }
        }

        ///<summary>Called by EventHandler, when the serverprocess exits</summary>
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