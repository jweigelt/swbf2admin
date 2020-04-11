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
        Stopping = 3,
        SteamPending = 4
    }

    public class ServerManager : ComponentBase
    {
        private const string DLLLOADER_FILENAME = "dllloader.exe";
        private const string SERVERPROC_NAME = "BattlefrontII";
        private const int STEAMMODE_PDECT_TIMEOUT = 1000;
        private const int STEAMMODE_MAX_RETRY = 30;

        public event EventHandler ServerCrashed;
        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;
        public event EventHandler SteamServerStarting;

        public string ServerPath { get; set; } = "./server";
        public string ServerArgs { get; set; } = "/win /norender /nosound /autonet dedicated /resolution 640 480";

        private Process serverProcess = null;
        private ServerStatus status = ServerStatus.Offline;
        public ServerStatus Status { get { return status; } }
        public ServerSettings Settings { get; set; }
        public virtual Process ServerProcess { get { return serverProcess; } }

        private int steamLaunchRetryCount = 0;
        GameserverType serverType;

        public ServerManager(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            ServerPath = Core.Files.ParseFileName(config.ServerPath);
            serverType = config.ServerType;

            ServerArgs = (serverType == GameserverType.Steam ? string.Empty : config.ServerArgs);

            UpdateInterval = STEAMMODE_PDECT_TIMEOUT; //updates for detecting steam startup
        }

        public override void OnInit()
        {
            Attach(false);
            Settings = ServerSettings.FromSettingsFile(Core, ServerPath);
        }

        protected override void OnUpdate()
        {
            if (Status == ServerStatus.SteamPending)
            {
                if (Attach(true))
                {
                    DisableUpdates();
                    steamLaunchRetryCount = 0;
                }
                else if (++steamLaunchRetryCount > STEAMMODE_MAX_RETRY)
                {
                    Logger.Log(LogLevel.Error, "Server didn't start after {0} retries. Assuming it has crashed.", steamLaunchRetryCount.ToString());
                    status = ServerStatus.Offline;
                    DisableUpdates();
                }

            }
        }

        private Process FindProcess(string name)
        {
            foreach (Process p in Process.GetProcessesByName(name))
            {
                try
                {
                    //NOTE: as there's no easy way to detect steam startup, we assume we're already in running mode when re-attaching
                    if (Path.GetFullPath(p.MainModule.FileName).Equals(Path.GetFullPath(ServerPath + $"\\{name}.exe")))
                    {
                        Logger.Log(LogLevel.Info, "Found running server process '{0}' ({1}), re-attaching...", p.MainWindowTitle, p.Id.ToString());
                        return p;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, "Can't access BattlefrontII process #{0} ({1})", p.Id.ToString(), e.Message);
                }
            }
            return null;
        }

        private bool Attach(bool starting)
        {
            serverProcess = FindProcess(SERVERPROC_NAME);
            if (serverProcess != null)
            {
                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                status = ServerStatus.Online;

                InvokeEvent(ServerStarted, this, new StartEventArgs(!starting));
                if (starting) InjectRconDllIfRequired();
                if (Core.Config.EnableHighPriority)
                {
                    serverProcess.PriorityClass = ProcessPriorityClass.High;
                }
                return true;
            }
            return false;
        }

        public void Start()
        {
            if (serverProcess == null)
            {
                Logger.Log(LogLevel.Info, "Launching server with args '{0}'", ServerArgs);
                status = ServerStatus.Starting;

                Environment.SetEnvironmentVariable("SPAWN_TIMER", Core.Server.Settings.AutoAnnouncePeriod.ToString());

                ProcessStartInfo startInfo = new ProcessStartInfo(Core.Files.ParseFileName(ServerPath + "/BattlefrontII.exe"), ServerArgs)
                {
                    WorkingDirectory = Core.Files.ParseFileName(ServerPath)
                };

                //if we're in steam mode, steam will start at launcher exe prior to the actual game
                if (serverType == GameserverType.Steam)
                {
                    InvokeEvent(SteamServerStarting, this, new EventArgs());
                    steamLaunchRetryCount = 0;
                    Core.Scheduler.PushDelayedTask(() =>
                    {
                        serverProcess = Process.Start(startInfo);
                        serverProcess.EnableRaisingEvents = true;
                        serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                    }
                    , 5000);
                    status = ServerStatus.SteamPending;
                }
                else
                {
                    serverProcess = Process.Start(startInfo);
                    serverProcess.EnableRaisingEvents = true;
                    serverProcess.Exited += new EventHandler(ServerProcess_Exited);

                    status = ServerStatus.Online;
                    InvokeEvent(ServerStarted, this, new StartEventArgs(false));
                    InjectRconDllIfRequired();
                }
            }
        }

        public void Stop(ServerStopReason reason = ServerStopReason.STOP_EXIT)
        {
            if (serverProcess != null)
            {
                Logger.Log(LogLevel.Info, "Stopping Server...");
                status = ServerStatus.Stopping;
                //Note: moved this from SP_Exited to give runtime advanced notice so we dont cause exceptions
                if (Core.Config.EnableRuntime)
                {
                    Core.Scheduler.PushTask(() => { Core.Rcon.SendCommand("shutdown"); });
                    Core.Scheduler.PushDelayedTask(() => KillServer(reason), 1000);
                }
                else KillServer(reason);
            }
        }

        public void Restart()
        {
            Stop(ServerStopReason.STOP_RESTART);
        }

        private void KillServer(ServerStopReason reason)
        {
            if (!serverProcess.HasExited)
            {
                serverProcess.Kill();
            }
            InvokeEvent(ServerStopped, this, new StopEventArgs(reason));
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            Process p = serverProcess;
            serverProcess = null;

            if (status != ServerStatus.Stopping && status != ServerStatus.SteamPending)
            {
                Logger.Log(LogLevel.Warning, "Server has crashed.");
                status = ServerStatus.Offline;
                InvokeEvent(ServerCrashed, this, new EventArgs());
            }
            else if (status == ServerStatus.SteamPending)
            {
                Logger.Log(LogLevel.Info, "Steam Launcher closed. Trying to attach to the server process.");
                EnableUpdates();
            }
            else
            {
                Logger.Log(LogLevel.Info, "Server stopped.");
                status = ServerStatus.Offline;
            }
        }

        private void InjectRconDllIfRequired()
        {
            if (serverType != GameserverType.Gamespy)
            {
                string loader = $"{Core.Files.ParseFileName(Core.Config.ServerPath)}/{DLLLOADER_FILENAME}";
                if (File.Exists(loader))
                {
                    Process.Start(loader, string.Format("--pid {0} --dll {1}", serverProcess.Id, "RconServer.dll"));
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Can't find {0}", loader);
                }
                return;
            }
        }
    }
}