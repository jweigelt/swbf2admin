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

namespace SWBF2Admin.Runtime.Watchdog
{
    public class ScheduledRestart : ComponentBase
    {
        private enum RestartState
        {
            Waiting,
            WaitingForMapEnd,
            RestartQueued
        }

        private ScheduleConfiguration config;
        private DateTime serverStartTime;
        private DateTime nextAnnouncementTime;
        private RestartState state;
        private int restartRequestId;

        private const int GAME_END_RESTART_DELAY = 5000; //let clients receive end-of-game packets before restart

        public ScheduledRestart(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            ReloadConfig();
        }

        /// <summary>
        /// Reloads schedule.xml and applies the new settings.
        /// </summary>
        public void ReloadConfig()
        {
            config = Core.Files.ReadConfig<ScheduleConfiguration>();
            UpdateInterval = config.CheckInterval;

            if (!config.EnableScheduledRestart)
            {
                ResetSchedule();
                DisableUpdates();
                return;
            }

            if (Core.Server.Status == ServerStatus.Online)
            {
                EnableUpdates();
                ReconcileSchedule();
            }
        }

        public override void OnInit()
        {
            //Restart on map change, not mid-game
            Core.Rcon.GameEnded += new EventHandler(Server_GameEnded);
        }

        public override void OnDeInit()
        {
            Core.Rcon.GameEnded -= new EventHandler(Server_GameEnded);
            ResetSchedule();
            DisableUpdates();
        }

        public override void OnServerStart(EventArgs e)
        {
            ResetSchedule();
            serverStartTime = DateTime.Now;
            if (config.EnableScheduledRestart) EnableUpdates();
        }

        public override void OnServerStop()
        {
            ResetSchedule();
            serverStartTime = DateTime.MinValue;
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            if (state == RestartState.Waiting && RestartIsDue())
            {
                WaitForMapEnd();
            }

            if (state == RestartState.WaitingForMapEnd) Announce();
        }

        private void Announce()
        {
            if (!config.EnableRestartAnnouncement || string.IsNullOrEmpty(config.RestartAnnouncement)) return;
            if (Core.Players.PlayerList.Count < 1) return;
            if (DateTime.Now < nextAnnouncementTime) return;

            Core.Rcon.Say(config.RestartAnnouncement);
            nextAnnouncementTime = DateTime.Now.AddSeconds(config.AnnouncementInterval);
        }

        private void Server_GameEnded(object sender, EventArgs e)
        {
            if (state == RestartState.WaitingForMapEnd)
            {
                Logger.Log(LogLevel.Info, "Map ended - performing scheduled restart");
                state = RestartState.RestartQueued;
                int requestId = ++restartRequestId;
                Core.Scheduler.PushDelayedTask(() => RestartServer(requestId), GAME_END_RESTART_DELAY);
            }
        }

        private void RestartServer(int requestId)
        {
            if (requestId != restartRequestId ||
                state != RestartState.RestartQueued ||
                !config.EnableScheduledRestart ||
                Core.Server.Status != ServerStatus.Online)
            {
                return;
            }

            state = RestartState.Waiting;
            DisableUpdates();
            Core.Server.Restart();
        }

        private void ReconcileSchedule()
        {
            if (serverStartTime == DateTime.MinValue) return;

            if (!RestartIsDue())
            {
                ResetSchedule();
            }
            else if (state == RestartState.Waiting)
            {
                WaitForMapEnd();
            }
        }

        private void WaitForMapEnd()
        {
            Logger.Log(LogLevel.Info, "Server has been running for {0} seconds - scheduling a restart for the next map change", config.RestartThreshold.ToString());
            state = RestartState.WaitingForMapEnd;
            nextAnnouncementTime = DateTime.Now;
        }

        private bool RestartIsDue()
        {
            return serverStartTime != DateTime.MinValue &&
                (DateTime.Now - serverStartTime).TotalSeconds >= config.RestartThreshold;
        }

        private void ResetSchedule()
        {
            ++restartRequestId;
            state = RestartState.Waiting;
            nextAnnouncementTime = DateTime.MinValue;
        }
    }
}
