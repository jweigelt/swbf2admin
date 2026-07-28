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
    /// Restarts the server, then Steam if rcon remains unresponsive.
    /// </summary>
    public class SteamRecovery : ComponentBase
    {
        private enum RecoveryState
        {
            Monitoring,
            VerifyingServerRestart,
            RestartingSteam,
            VerifyingSteamRestart,
            Failed
        }

        private const int STEAM_SHUTDOWN_WAIT = 10000;   //ms to let 'steam -shutdown' finish
        private const int STEAM_LOGIN_WAIT = 30000;      //ms to let Steam relaunch + log back in

        private int graceSeconds;
        private int cooldownSeconds;
        private string steamExe;

        private DateTime serverStartTime;
        private DateTime lastSteamRestart = DateTime.MinValue;
        private RecoveryState state = RecoveryState.Monitoring;
        private int recoveryRequestId;

        public SteamRecovery(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            UpdateInterval = config.SteamRecoveryCheckInterval;
            graceSeconds = config.SteamRecoveryGraceSeconds;
            cooldownSeconds = config.SteamRecoveryCooldownSeconds;
            steamExe = Path.Combine(config.SteamPath, "steam.exe");
        }

        public override void OnServerStart(EventArgs e)
        {
            serverStartTime = DateTime.Now;

            if (state == RecoveryState.RestartingSteam)
            {
                ResetRecovery();
            }

            EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();

            if (state == RecoveryState.RestartingSteam)
            {
                ShutdownSteam();
            }
            else if (state == RecoveryState.Monitoring || state == RecoveryState.Failed)
            {
                ++recoveryRequestId;
            }
        }

        public override void OnDeInit()
        {
            ++recoveryRequestId;
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            Process process = Core.Server.ServerProcess;
            if (process == null || process.HasExited ||
                Core.Server.Status != ServerStatus.Online)
            {
                return;
            }

            if (RconIsResponding())
            {
                if (state != RecoveryState.Monitoring)
                {
                    Logger.Log(LogLevel.Info, "Rcon is responding again. Recovery reset.");
                    ResetRecovery();
                }
                return;
            }

            if ((DateTime.Now - serverStartTime).TotalSeconds <= graceSeconds) return;

            switch (state)
            {
                case RecoveryState.Monitoring:
                    Logger.Log(LogLevel.Warning,
                        "Rcon has not responded for {0}s. Restarting server (step 1/3).",
                        graceSeconds.ToString());
                    state = RecoveryState.VerifyingServerRestart;
                    Core.Server.Restart();
                    break;

                case RecoveryState.VerifyingServerRestart:
                    RestartSteam();
                    break;

                case RecoveryState.VerifyingSteamRestart:
                    FailRecovery("Rcon did not recover after restarting Steam. Recovery paused until Rcon responds (step 3/3).");
                    break;

                case RecoveryState.Failed:
                    break;
            }
        }

        public void CancelRecovery()
        {
            ++recoveryRequestId;
            if (state != RecoveryState.Failed)
            {
                state = RecoveryState.Monitoring;
            }

            if (Core.Server.Status == ServerStatus.Online) EnableUpdates();
            else DisableUpdates();
        }

        private bool RconIsResponding()
        {
            //Use /status to check whether rcon is responding
            DateTime lastResponse = Core.Rcon.LastSuccessfulStatusResponse;
            return lastResponse >= serverStartTime &&
                (DateTime.Now - lastResponse).TotalSeconds <= graceSeconds;
        }

        private void RestartSteam()
        {
            double cooldownRemaining = cooldownSeconds -
                (DateTime.Now - lastSteamRestart).TotalSeconds;
            if (cooldownRemaining > 0)
            {
                FailRecovery(string.Format(
                    "Steam recovery was not repeated because the previous Steam restart was too recent ({0}s remaining).",
                    Math.Ceiling(cooldownRemaining)));
                return;
            }

            Logger.Log(LogLevel.Warning,
                "Rcon is still not responding after the server restart. Restarting Steam and the server (step 2/3).");
            state = RecoveryState.RestartingSteam;
            lastSteamRestart = DateTime.Now;
            ++recoveryRequestId;
            //Steam will not shut down while a game it launched is running
            Core.Server.Stop(ServerStopReason.STOP_EXIT);
        }

        private void ShutdownSteam()
        {
            if (state != RecoveryState.RestartingSteam) return;

            if (!File.Exists(steamExe))
            {
                FailRecovery(string.Format("Steam recovery failed because '{0}' does not exist.", steamExe));
                return;
            }

            Logger.Log(LogLevel.Info, "Steam recovery: shutting down Steam.");
            if (!StartSteam("-shutdown", "Steam shutdown")) return;

            int requestId = recoveryRequestId;
            Core.Scheduler.PushDelayedTask(() => RelaunchSteam(requestId), STEAM_SHUTDOWN_WAIT);
        }

        private void RelaunchSteam(int requestId)
        {
            if (!RecoveryIsCurrent(requestId) ||
                !TryGetSteamRunning(out bool steamRunning)) return;
            if (steamRunning)
            {
                FailRecovery("Steam recovery failed because Steam did not shut down.");
                return;
            }

            Logger.Log(LogLevel.Info, "Steam recovery: starting Steam.");
            if (!StartSteam("-silent", "Steam relaunch")) return;

            Core.Scheduler.PushDelayedTask(() => RestartServer(requestId), STEAM_LOGIN_WAIT);
        }

        private void RestartServer(int requestId)
        {
            if (!RecoveryIsCurrent(requestId)) return;

            if (Core.Server.Status != ServerStatus.Offline)
            {
                FailRecovery(string.Format(
                    "Steam recovery could not start the server while it was {0}.",
                    Core.Server.Status));
                return;
            }

            Logger.Log(LogLevel.Info, "Steam recovery: starting server.");
            state = RecoveryState.VerifyingSteamRestart;
            Core.Server.Start();

            if (Core.Server.Status == ServerStatus.Offline)
            {
                FailRecovery("Steam recovery failed to start the server.");
            }
        }

        private bool TryGetSteamRunning(out bool running)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("steam");
                running = processes.Length > 0;
                foreach (Process process in processes) process.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                running = false;
                FailRecovery(string.Format("Steam recovery could not check the Steam process ({0})", ex.Message));
                return false;
            }
        }

        private bool StartSteam(string arguments, string action)
        {
            try
            {
                using Process command = Process.Start(steamExe, arguments);
                if (command != null) return true;
                FailRecovery(action + " failed because Process.Start returned null.");
            }
            catch (Exception ex)
            {
                FailRecovery(string.Format("{0} failed ({1})", action, ex.Message));
            }
            return false;
        }

        private bool RecoveryIsCurrent(int requestId)
        {
            return requestId == recoveryRequestId &&
                state == RecoveryState.RestartingSteam;
        }

        private void ResetRecovery()
        {
            ++recoveryRequestId;
            state = RecoveryState.Monitoring;
        }

        private void FailRecovery(string message)
        {
            ++recoveryRequestId;
            state = RecoveryState.Failed;
            Logger.Log(LogLevel.Error, message);
        }
    }
}
