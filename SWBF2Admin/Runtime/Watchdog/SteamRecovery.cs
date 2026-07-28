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
using SWBF2Admin.Gameserver;
using SWBF2Admin.Utility;
using System;
using System.Diagnostics;
using System.IO;

namespace SWBF2Admin.Runtime.Watchdog
{
    /// <summary>
    /// Aspyr/Steam only: recovers from a stale Steam session. If the server is up but rcon
    /// stops responding, restarts it; if still unresponsive, cycles the Steam client once;
    /// if still unresponsive, gives up (no restart loop).
    /// </summary>
    public class SteamRecovery : ComponentBase
    {
        private const int STAGE_HEALTHY = 0;
        private const int STAGE_RESTARTED = 1;
        private const int STAGE_STEAM_CYCLED = 2;
        private const int STAGE_GAVE_UP = 3;

        private const int GAME_STOP_WAIT = 5000;         //ms to let the game exit before shutting Steam
        private const int STEAM_SHUTDOWN_WAIT = 10000;   //ms to let 'steam -shutdown' finish
        private const int STEAM_LOGIN_WAIT = 30000;      //ms to let Steam relaunch + log back in

        //failsafe so a never-completing restart can't wedge the ladder
        private const int RECOVERY_TIMEOUT = GAME_STOP_WAIT + STEAM_SHUTDOWN_WAIT + STEAM_LOGIN_WAIT + 120000;

        private int graceSeconds;
        private int cooldownSeconds;
        private string steamPath;

        private DateTime serverStartTime;
        private DateTime lastSteamCycle = DateTime.MinValue;
        private int stage = STAGE_HEALTHY;
        private bool firstTickLogged;
        private bool postGraceLogged;

        public SteamRecovery(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            UpdateInterval = config.SteamRecoveryCheckInterval;
            graceSeconds = config.SteamRecoveryGraceSeconds;
            cooldownSeconds = config.SteamRecoveryCooldownSeconds;
            steamPath = config.SteamPath;

            Logger.Log(LogLevel.Info,
                "Steam recovery configured: grace={0}s, cooldown={1}s, check={2}ms, steamPath=\"{3}\".",
                graceSeconds.ToString(), cooldownSeconds.ToString(), UpdateInterval.ToString(), steamPath);
        }

        public override void OnServerStart(EventArgs e)
        {
            //keep 'stage' across restarts so the ladder can escalate; only rcon recovery resets it
            serverStartTime = DateTime.Now;
            firstTickLogged = false;
            postGraceLogged = false;
            EnableUpdates();

            LogDiagnosticState("armed");
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            if (!firstTickLogged)
            {
                firstTickLogged = true;
                LogDiagnosticState("first tick");
            }

            if (!postGraceLogged &&
                (DateTime.Now - serverStartTime).TotalSeconds > graceSeconds)
            {
                postGraceLogged = true;
                LogDiagnosticState("post-grace evaluation");
            }

            //only while the process is up and online (crashes are handled by AutoRestart)
            Process proc = Core.Server.ServerProcess;
            if (proc == null || proc.HasExited || Core.Server.Status != ServerStatus.Online)
                return;

            DateTime lastSuccessfulStatusResponse = Core.Rcon.LastSuccessfulStatusResponse;
            if (lastSuccessfulStatusResponse != DateTime.MinValue &&
                (DateTime.Now - lastSuccessfulStatusResponse).TotalSeconds <= graceSeconds)
            {
                if (stage != STAGE_HEALTHY)
                {
                    Logger.Log(LogLevel.Info, "Validated Rcon status response received - recovery reset.");
                    stage = STAGE_HEALTHY;
                }
                return;
            }

            //give each (re)start a grace window before acting
            if ((DateTime.Now - serverStartTime).TotalSeconds <= graceSeconds) return;

            switch (stage)
            {
                case STAGE_HEALTHY:
                    Logger.Log(LogLevel.Warning, "Server is online but Rcon hasn't responded for >{0}s - restarting server (step 1/3).", graceSeconds.ToString());
                    stage = STAGE_RESTARTED;
                    Core.Server.Restart();
                    break;

                case STAGE_RESTARTED:
                    if ((DateTime.Now - lastSteamCycle).TotalSeconds < cooldownSeconds)
                        return; //within Steam-cycle cooldown - wait
                    Logger.Log(LogLevel.Warning, "Rcon still not responding after restart - cycling Steam, then restarting (step 2/3).");
                    stage = STAGE_STEAM_CYCLED;
                    lastSteamCycle = DateTime.Now;

                    LogSteamProcessSnapshot("cycle scheduled");
                    Core.Server.Stop(ServerStopReason.STOP_EXIT);
                    Core.Scheduler.PushDelayedTask(ShutdownSteam, GAME_STOP_WAIT);

                    Core.Scheduler.PushDelayedTask(AbortOnFailure, RECOVERY_TIMEOUT);
                    break;

                case STAGE_STEAM_CYCLED: //ladder exhausted
                    AbortOnFailure();
                    break;
            }
        }

        private void LogDiagnosticState(string checkpoint)
        {
            DateTime now = DateTime.Now;
            string processState;

            try
            {
                Process process = Core.Server.ServerProcess;
                processState = process == null
                    ? "process=<null>"
                    : string.Format("pid={0}, exited={1}", process.Id, process.HasExited);
            }
            catch (Exception ex)
            {
                processState = "process=<" + ex.GetType().Name + ">";
            }

            string lastRxAge = Core.Rcon.LastRx == DateTime.MinValue
                ? "never"
                : Math.Max(0, (now - Core.Rcon.LastRx).TotalSeconds).ToString("F1") + "s";
            DateTime lastSuccessfulStatusResponse = Core.Rcon.LastSuccessfulStatusResponse;
            string lastValidStatusAge = lastSuccessfulStatusResponse == DateTime.MinValue
                ? "never"
                : Math.Max(0, (now - lastSuccessfulStatusResponse).TotalSeconds).ToString("F1") + "s";
            double serverAge = Math.Max(0, (now - serverStartTime).TotalSeconds);

            Logger.Log(LogLevel.Info,
                "Steam recovery diagnostic ({0}): status={1}, {2}, stage={3}, serverAge={4}s, lastRxAge={5}, lastValidStatusAge={6}.",
                checkpoint, Core.Server.Status.ToString(), processState, stage.ToString(),
                serverAge.ToString("F1"), lastRxAge, lastValidStatusAge);
        }

        //single timer fired after the Steam cycle: disable the watchdog if rcon still hasn't recovered
        private void AbortOnFailure()
        {
            if (stage == STAGE_GAVE_UP || stage == STAGE_HEALTHY) return;
            Logger.Log(LogLevel.Error, "Server/Rcon didn't recover after Steam cycle - aborting.");
            stage = STAGE_GAVE_UP;
            DisableUpdates();
        }

        private void ShutdownSteam()
        {
            LogSteamProcessSnapshot("before shutdown command");
            Logger.Log(LogLevel.Info, "Steam recovery: executing steam.exe -shutdown.");
            try
            {
                using Process command = Process.Start(Path.Combine(steamPath, "steam.exe"), "-shutdown");
                Logger.Log(LogLevel.Info, "Steam shutdown command launched: pid={0}.",
                    command == null ? "<null>" : command.Id.ToString());
            }
            catch (Exception ex) { Logger.Log(LogLevel.Warning, "Steam shutdown failed ({0})", ex.Message); }

            //relaunch Steam after 10s
            Core.Scheduler.PushDelayedTask(RelaunchSteam, STEAM_SHUTDOWN_WAIT);
        }

        private void RelaunchSteam()
        {
            LogSteamProcessSnapshot("after shutdown wait");
            Logger.Log(LogLevel.Info, "Steam recovery: executing steam.exe -silent.");
            try
            {
                using Process command = Process.Start(Path.Combine(steamPath, "steam.exe"), "-silent");
                Logger.Log(LogLevel.Info, "Steam relaunch command launched: pid={0}.",
                    command == null ? "<null>" : command.Id.ToString());
            }
            catch (Exception ex) { Logger.Log(LogLevel.Warning, "Steam relaunch failed ({0})", ex.Message); }

            //restart the server after 30s
            Core.Scheduler.PushDelayedTask(RestartServerAfterSteamCycle, STEAM_LOGIN_WAIT);
        }

        private void RestartServerAfterSteamCycle()
        {
            LogSteamProcessSnapshot("before server restart");
            Logger.Log(LogLevel.Info, "Steam recovery: Steam login wait complete; restarting server.");
            Core.Server.Start();
        }

        private void LogSteamProcessSnapshot(string checkpoint)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("steam");
                if (processes.Length == 0)
                {
                    Logger.Log(LogLevel.Info, "Steam process diagnostic ({0}): no steam.exe process found.", checkpoint);
                    return;
                }

                string[] details = new string[processes.Length];
                for (int i = 0; i < processes.Length; ++i)
                {
                    Process process = processes[i];
                    try
                    {
                        details[i] = string.Format("pid={0}, session={1}, started={2}",
                            process.Id, process.SessionId, process.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    }
                    catch (Exception ex)
                    {
                        details[i] = string.Format("pid={0}, details=<{1}>", process.Id, ex.GetType().Name);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                Logger.Log(LogLevel.Info, "Steam process diagnostic ({0}): {1}.",
                    checkpoint, string.Join("; ", details));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Steam process diagnostic ({0}) failed ({1})",
                    checkpoint, ex.Message);
            }
        }
    }
}
