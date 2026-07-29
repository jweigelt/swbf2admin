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
using SWBF2Admin.Config;
using SWBF2Admin.Structures;
using SWBF2Admin.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

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
        private const string DLLLOADER_FILENAME_32 = "DllLoader_32.exe";
        private const string DLLLOADER_FILENAME_64 = "DllLoader_64.exe";
        private const string ASPYR_PID_FILE = "BattlefrontII.pid";
        private const int DIRECT_TRANSPORT_FAILURE_EXIT_CODE = 0xD1;
        private const int STEAMMODE_PDECT_TIMEOUT = 1000;
        private const int STEAMMODE_MAX_RETRY = 30;

        public event EventHandler ServerCrashed;
        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;
        public event EventHandler SteamServerStarting;

        public string ServerExecutable { get; set; } = "BattlefrontII.exe";
        public string ServerProcessName { get; set; } = "BattlefrontII";
        public string ServerPath { get; set; } = "./server";
        public string ServerArgs { get; set; } = "/win /norender /nosound /nointro /autonet dedicated /resolution 640 480";

        private Process serverProcess = null;
        private ServerStatus status = ServerStatus.Offline;
        public ServerStatus Status { get { return status; } }
        private ServerStopReason stopReason = ServerStopReason.STOP_EXIT;
        public ServerSettings Settings { get; set; }
        public virtual Process ServerProcess { get { return serverProcess; } }

        private int steamLaunchRetryCount = 0;
        private GameserverType serverType;
        private int startRequestId;

        public ServerManager(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            ServerPath = Core.Files.ParseFileName(config.ServerPath);
            serverType = config.ServerType;
            
            if (serverType == GameserverType.Steam)
            {
                ServerExecutable = ServerPath + "/BattlefrontII.exe";
                ServerArgs = "";
            }
            if (serverType == GameserverType.Aspyr)
            {
                ServerExecutable = config.ServerPath + "/Battlefront.exe";
                ServerArgs = config.ServerArgs;
                var appid_txt = Path.GetFullPath(ServerPath + "/steam_appid.txt");
                if (!File.Exists(appid_txt))
                {
                    Core.Files.WriteFileText(appid_txt, "2446550");
                }
                ServerProcessName = "Battlefront";
                Core.Rcon.SetPacketTimeout(1500);
            }
            else
            {
                ServerExecutable = ServerPath + "/BattlefrontII.exe";
                ServerArgs = config.ServerArgs;
                Core.Rcon.SetPacketTimeout(500);
            }
         
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
                    HandleStartFailure(string.Format(
                        "Server didn't start after {0} retries.",
                        steamLaunchRetryCount));
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

        private Process FindProcessByPidFile()
        {
            //Use Aspyr's PID file to reattach to this server instance
            string pidFile = Path.GetFullPath(Path.Combine(ServerPath, "settings", ASPYR_PID_FILE));
            if (!File.Exists(pidFile))
            {
                return null;
            }

            try
            {
                if (!int.TryParse(File.ReadAllText(pidFile).Trim(), out int pid))
                {
                    Logger.Log(LogLevel.Warning, "Ignoring malformed pid file '{0}'", pidFile);
                    return null;
                }

                Process p = Process.GetProcessById(pid);
                if (!p.ProcessName.Equals(ServerProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    //Ignore the PID if it now belongs to another process
                    p.Dispose();
                    return null;
                }

                Logger.Log(LogLevel.Info, "Found running server process '{0}' ({1}) via pid file, re-attaching...", p.MainWindowTitle, p.Id.ToString());
                return p;
            }
            catch (ArgumentException)
            {
                //The PID file can remain after the server exits
                return null;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Can't re-attach via pid file '{0}' ({1})", pidFile, e.Message);
                return null;
            }
        }

        private bool Attach(bool starting)
        {
            Process process = (serverType == GameserverType.Aspyr)
                ? FindProcessByPidFile()
                : FindProcess(ServerProcessName);
            if (process != null)
            {
                Interlocked.Increment(ref startRequestId);
                serverProcess = process;
                process.EnableRaisingEvents = true;
                process.Exited += new EventHandler(ServerProcess_Exited);
                status = ServerStatus.Online;

                ApplyProcessSettings(process);
                if (!ProcessIsActive(process)) return false;

                InvokeEvent(ServerStarted, this, new StartEventArgs(!starting));
                if (starting) InjectRconDllIfRequired(process);
                return true;
            }
            return false;
        }

        public void Start()
        {
            if (status != ServerStatus.Offline || serverProcess != null)
            {
                return;
            }

            int requestId = Interlocked.Increment(ref startRequestId);
            status = ServerStatus.Starting;

            ProcessStartInfo startInfo;
            try
            {
                startInfo = CreateStartInfo();
            }
            catch (Exception ex)
            {
                HandleStartFailure(ex.Message);
                return;
            }

            Logger.Log(LogLevel.Info, "Launching server with args '{0}'", startInfo.Arguments);

            //if we're in steam mode, steam will start a launcher exe prior to the actual game
            if (serverType == GameserverType.Steam)
            {
                InvokeEvent(SteamServerStarting, this, new EventArgs());
                steamLaunchRetryCount = 0;
                status = ServerStatus.SteamPending;
                Core.Scheduler.PushDelayedTask(() =>
                {
                    if (requestId != Volatile.Read(ref startRequestId) ||
                        status != ServerStatus.SteamPending)
                    {
                        return;
                    }

                    try
                    {
                        serverProcess = Process.Start(startInfo);
                        if (serverProcess == null)
                        {
                            throw new InvalidOperationException("Process.Start returned null.");
                        }
                        serverProcess.EnableRaisingEvents = true;
                        serverProcess.Exited += new EventHandler(ServerProcess_Exited);
                    }
                    catch (Exception ex)
                    {
                        HandleStartFailure(ex.Message);
                    }
                }, 5000);
            }
            else
            {
                Process process;
                try
                {
                    process = Process.Start(startInfo);
                    if (process == null)
                    {
                        throw new InvalidOperationException("Process.Start returned null.");
                    }
                    serverProcess = process;
                    status = ServerStatus.Online;
                    process.EnableRaisingEvents = true;
                    process.Exited += new EventHandler(ServerProcess_Exited);
                }
                catch (Exception ex)
                {
                    HandleStartFailure(ex.Message);
                    return;
                }

                ApplyProcessSettings(process);
                if (!ProcessIsActive(process)) return;
                InvokeEvent(ServerStarted, this, new StartEventArgs(false));
                InjectRconDllIfRequired(process);
            }
        }

        public void Stop(ServerStopReason reason = ServerStopReason.STOP_EXIT)
        {
            Interlocked.Increment(ref startRequestId);
            if (serverProcess != null)
            {
                Logger.Log(LogLevel.Info, "Stopping Server...");
                bool sendShutdown = Core.Config.EnableRuntime && status == ServerStatus.Online;
                status = ServerStatus.Stopping;
                stopReason = reason;

                Process process = serverProcess;
                if (sendShutdown)
                {
                    Logger.Log(LogLevel.Verbose, "Asking server to stop");
                    Core.Scheduler.PushTask(() => { Core.Rcon.SendCommand("shutdown"); });
                    Core.Scheduler.PushDelayedTask(() => KillServer(process), 1000);
                }
                else KillServer(process);
            }
            else
            {
                status = ServerStatus.Offline;
                DisableUpdates();
                if (reason == ServerStopReason.STOP_RESTART)
                {
                    ScheduleStart();
                }
            }
        }

        public void Restart()
        {
            Stop(ServerStopReason.STOP_RESTART);
        }

        private void KillServer(Process process)
        {
            if (process == null) return;

            try
            {
                if (!process.HasExited)
                {
                    Logger.Log(LogLevel.Verbose, "Stopping process...");
                    process.Kill();
                }
            }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Failed to stop server process. ({0})", ex.Message);
            }
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            Process exitedProcess = sender as Process;
            if (exitedProcess == null || !ReferenceEquals(exitedProcess, serverProcess)) return;

            serverProcess = null;

            if (status != ServerStatus.Stopping && status != ServerStatus.SteamPending)
            {
                bool directTransportFailure = false;
                try
                {
                    directTransportFailure = exitedProcess.ExitCode == DIRECT_TRANSPORT_FAILURE_EXIT_CODE;
                }
                catch (InvalidOperationException) { }
                if (directTransportFailure)
                {
                    Logger.Log(LogLevel.Error, "Direct transport startup failed.");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "Server has crashed.");
                }
                status = ServerStatus.Offline;
                InvokeEvent(ServerCrashed, this, new EventArgs());
                if (Core.Config.AutoRestartServer)
                {
                    Logger.Log(LogLevel.Info, "Automatic restart is enabled. Restarting server...");
                    ScheduleStart();
                }
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
                InvokeEvent(ServerStopped, this, new StopEventArgs(stopReason));
                if (stopReason == ServerStopReason.STOP_RESTART)
                {
                    Logger.Log(LogLevel.Verbose, "Restarting server...");
                    ScheduleStart();
                }
            }
        }

        private void HandleStartFailure(string message)
        {
            Interlocked.Increment(ref startRequestId);
            Process process = serverProcess;
            serverProcess = null;
            KillServer(process);
            status = ServerStatus.Offline;
            DisableUpdates();
            Logger.Log(LogLevel.Error, "Failed to start server. ({0})", message);
            InvokeEvent(ServerCrashed, this, new EventArgs());

            if (Core.Config.AutoRestartServer)
            {
                Logger.Log(LogLevel.Info, "Automatic restart is enabled. Restarting server...");
                ScheduleStart();
            }
        }

        private void ScheduleStart()
        {
            int requestId = Interlocked.Increment(ref startRequestId);
            Core.Scheduler.PushDelayedTask(() =>
            {
                if (requestId != Volatile.Read(ref startRequestId) ||
                    status != ServerStatus.Offline || serverProcess != null)
                {
                    return;
                }

                Start();
            }, Core.Config.AutoRestartDelay);
        }

        private ProcessStartInfo CreateStartInfo()
        {
            string processArgs = ServerArgs;
            if (serverType == GameserverType.Aspyr)
            {
                //Aspyr servers become unstable with /norender
                processArgs = processArgs.Replace("/norender", "",
                    StringComparison.OrdinalIgnoreCase);
                processArgs += " /bf2";
                //processArgs += " /netregion \"" + Core.Server.Settings.NetRegion + "\"";
                if (!string.IsNullOrEmpty(Core.Server.Settings.Password))
                {
                    processArgs += " /password \"" + Core.Server.Settings.Password + "\"";
                }
            }

            Environment.SetEnvironmentVariable("SPAWN_TIMER", Core.Server.Settings.AutoAnnouncePeriod.ToString());
            if (serverType == GameserverType.Aspyr)
            {
                Environment.SetEnvironmentVariable("PLATFORM_LOBBY",
                    Core.Server.Settings.Platform?.ToLowerInvariant());
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(
                Core.Files.ParseFileName(ServerExecutable), processArgs)
            {
                WorkingDirectory = Core.Files.ParseFileName(ServerPath)
            };
            startInfo.Environment["BF2_DIRECT_POLICY"] =
                Core.Config.DirectTransportPolicy.ToString();
            return startInfo;
        }

        private void ApplyProcessSettings(Process process)
        {
            try
            {
                if (Core.Config.EnableHighPriority)
                {
                    process.PriorityClass = ProcessPriorityClass.High;
                }
                if (Core.Config.SetAffinity)
                {
                    process.ProcessorAffinity = (IntPtr)Core.Config.ProcessAffinity;
                    Logger.Log(LogLevel.Info, "Process Affinity: 0x{0}", process.ProcessorAffinity.ToString("X"));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Failed to configure server process. ({0})", ex.Message);
            }
        }

        private bool ProcessIsActive(Process process)
        {
            if (!ReferenceEquals(process, serverProcess)) return false;

            try
            {
                if (!process.HasExited) return true;
            }
            catch (InvalidOperationException) { }

            ServerProcess_Exited(process, EventArgs.Empty);
            return false;
        }

        private void InjectRconDllIfRequired(Process process)
        {
            if (serverType != GameserverType.GoG &&
                serverType != GameserverType.Aspyr)
            {
                return;
            }

            string loader;
            string dll;
            if (serverType == GameserverType.Aspyr)
            {
                loader = Path.Combine(ServerPath, DLLLOADER_FILENAME_64);
                dll = "RconServer_64.dll";
            }
            else
            {
                loader = Path.Combine(ServerPath, DLLLOADER_FILENAME_32);
                dll = "RconServer_32.dll";
            }

            if (File.Exists(loader))
            {
                using Process loaderProcess = Process.Start(loader,
                    string.Format("--pid {0} --dll {1}", process.Id, dll));
            }
            else
            {
                Logger.Log(LogLevel.Error, "Can't find {0}", loader);
            }
        }
    }
}
