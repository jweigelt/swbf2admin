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
        private const int ACTION_PENDING_TIMEOUT = GAME_STOP_WAIT + STEAM_SHUTDOWN_WAIT + STEAM_LOGIN_WAIT + 120000;

        private int graceSeconds;
        private int cooldownSeconds;
        private string steamPath;

        private DateTime serverStartTime;
        private DateTime lastSteamCycle = DateTime.MinValue;
        private DateTime actionStartedTime = DateTime.MinValue;
        private int stage = STAGE_HEALTHY;
        private bool actionPending;

        public SteamRecovery(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            UpdateInterval = config.SteamRecoveryCheckInterval;
            graceSeconds = config.SteamRecoveryGraceSeconds;
            cooldownSeconds = config.SteamRecoveryCooldownSeconds;
            steamPath = config.SteamPath;
        }

        public override void OnServerStart(EventArgs e)
        {
            //keep 'stage' across restarts so the ladder can escalate; only rcon recovery resets it
            serverStartTime = DateTime.Now;
            actionPending = false;
            EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            //only while the process is up and online (crashes are handled by AutoRestart)
            Process proc = Core.Server.ServerProcess;
            if (proc == null || proc.HasExited || Core.Server.Status != ServerStatus.Online)
                return;

            if ((DateTime.Now - Core.Rcon.LastRx).TotalSeconds <= graceSeconds)
            {
                if (stage != STAGE_HEALTHY)
                {
                    Logger.Log(LogLevel.Info, "Rcon responding again - recovery reset.");
                    stage = STAGE_HEALTHY;
                }
                return;
            }

            //give each (re)start a grace window; don't act while a restart is in flight
            if (actionPending)
            {
                if ((DateTime.Now - actionStartedTime).TotalMilliseconds > ACTION_PENDING_TIMEOUT)
                {
                    Logger.Log(LogLevel.Warning, "Recovery action didn't complete in time - clearing pending state to allow escalation.");
                    actionPending = false;
                }
                else
                {
                    return;
                }
            }
            if ((DateTime.Now - serverStartTime).TotalSeconds <= graceSeconds) return;

            switch (stage)
            {
                case STAGE_HEALTHY:
                    Logger.Log(LogLevel.Warning, "Server is online but Rcon hasn't responded for >{0}s - restarting server (step 1/3).", graceSeconds.ToString());
                    stage = STAGE_RESTARTED;
                    actionPending = true;
                    actionStartedTime = DateTime.Now;
                    Core.Server.Restart();
                    break;

                case STAGE_RESTARTED:
                    if ((DateTime.Now - lastSteamCycle).TotalSeconds < cooldownSeconds)
                        return; //within Steam-cycle cooldown - wait
                    Logger.Log(LogLevel.Warning, "Rcon still not responding after restart - cycling Steam, then restarting (step 2/3).");
                    stage = STAGE_STEAM_CYCLED;
                    actionPending = true;
                    actionStartedTime = DateTime.Now;
                    lastSteamCycle = DateTime.Now;
                    CycleSteamThenRestart();
                    break;

                default: //STAGE_STEAM_CYCLED: ladder exhausted
                    Logger.Log(LogLevel.Error, "Rcon still not responding after Steam restart - giving up.");
                    stage = STAGE_GAVE_UP;
                    DisableUpdates();
                    break;
            }
        }

        private void CycleSteamThenRestart()
        {
            string steamExe = Path.Combine(steamPath, "steam.exe");

            //stop the game first - Steam won't shut down while a game it launched is running
            Core.Server.Stop(ServerStopReason.STOP_EXIT);

            Core.Scheduler.PushDelayedTask(() =>
            {
                try { Process.Start(steamExe, "-shutdown"); }
                catch (Exception ex) { Logger.Log(LogLevel.Warning, "Steam shutdown failed ({0})", ex.Message); }

                Core.Scheduler.PushDelayedTask(() =>
                {
                    try { Process.Start(steamExe); }
                    catch (Exception ex) { Logger.Log(LogLevel.Warning, "Steam relaunch failed ({0})", ex.Message); }

                    Core.Scheduler.PushDelayedTask(() => Core.Server.Start(), STEAM_LOGIN_WAIT);
                }, STEAM_SHUTDOWN_WAIT);
            }, GAME_STOP_WAIT);
        }
    }
}
