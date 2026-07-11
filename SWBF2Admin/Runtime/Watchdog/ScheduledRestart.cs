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
using SWBF2Admin.Utility;
using System;

namespace SWBF2Admin.Runtime.Watchdog
{
    public class ScheduledRestart : ComponentBase
    {
        private ScheduleConfiguration config;
        private DateTime serverStartTime;
        private DateTime lastAnnouncement;
        private bool restartPending;
        private bool isRestarting;

        public ScheduledRestart(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            ReloadConfig();
        }

        /// <summary>
        /// Re-reads schedule.xml so changes apply on the next server start
        /// </summary>
        public void ReloadConfig()
        {
            config = Core.Files.ReadConfig<ScheduleConfiguration>();
            UpdateInterval = config.CheckInterval;
        }

        public override void OnInit()
        {
            base.OnInit();
            //Restart on map change, not mid-game
            Core.Rcon.GameEnded += new EventHandler(Server_GameEnded);
        }

        public override void OnServerStart(EventArgs e)
        {
            restartPending = false;
            isRestarting = false;
            serverStartTime = DateTime.Now;
            lastAnnouncement = DateTime.Now;
            if (config.EnableScheduledRestart) EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            if (!restartPending && (DateTime.Now - serverStartTime).TotalSeconds > config.RestartThreshold)
            {
                Logger.Log(LogLevel.Info, "Server has been running for {0} seconds - scheduling a restart for the next map change", config.RestartThreshold.ToString());
                restartPending = true;
                lastAnnouncement = DateTime.MinValue;
            }

            if (restartPending) Announce();
        }

        private void Announce()
        {
            if (!config.EnableRestartAnnouncement || string.IsNullOrEmpty(config.RestartAnnouncement)) return;
            if (Core.Players.PlayerList.Count < 1) return;

            //Half a poll cycle of tolerance so a due announcement isn't skipped by timer jitter.
            double tolerance = (UpdateInterval / 1000.0) / 2.0;
            if ((DateTime.Now - lastAnnouncement).TotalSeconds < config.AnnouncementInterval - tolerance) return;

            Core.Rcon.Say(config.RestartAnnouncement);
            lastAnnouncement = DateTime.Now;
        }

        private void Server_GameEnded(object sender, EventArgs e)
        {
            if (restartPending && !isRestarting)
            {
                Logger.Log(LogLevel.Info, "Map ended - performing scheduled restart");
                isRestarting = true;
                Core.Server.Restart();
            }
        }
    }
}
