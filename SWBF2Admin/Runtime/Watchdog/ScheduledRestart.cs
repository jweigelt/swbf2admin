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
        private const int RESTART_CHECK_INTERVAL = 1000;
        private ScheduleConfiguration config;
        private DateTime serverStartTime;
        private DateTime lastAnnouncementTime;

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
            UpdateInterval = RESTART_CHECK_INTERVAL;

            if (!config.EnableScheduledRestart)
            {
                DisableUpdates();
                return;
            }

            if (Core.Server.Status == ServerStatus.Online) EnableUpdates();
        }

        public override void OnServerStart(EventArgs e)
        {
            serverStartTime = DateTime.Now;
            lastAnnouncementTime = DateTime.MinValue;
            if (config.EnableScheduledRestart) EnableUpdates();
        }

        public override void OnServerStop()
        {
            serverStartTime = DateTime.MinValue;
            lastAnnouncementTime = DateTime.MinValue;
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            if (serverStartTime == DateTime.MinValue) return;

            DateTime now = DateTime.Now;
            TimeSpan remaining = serverStartTime
                .AddMinutes(config.RestartThresholdMinutes) - now;

            if (remaining <= TimeSpan.Zero)
            {
                Logger.Log(LogLevel.Info,
                    "Server has been running for {0} minutes - performing scheduled restart",
                    config.RestartThresholdMinutes.ToString());
                DisableUpdates();
                Core.Server.Restart();
                return;
            }

            Announce(now, remaining);
        }

        private void Announce(DateTime now, TimeSpan remaining)
        {
            if (!config.EnableRestartAnnouncement ||
                string.IsNullOrEmpty(config.RestartAnnouncement) ||
                config.RestartCountdownMinutes < 1 ||
                remaining.TotalMinutes > config.RestartCountdownMinutes ||
                Core.Players.PlayerList.Count < 1)
            {
                return;
            }

            int interval = Math.Max(1, config.AnnouncementInterval);
            if (lastAnnouncementTime != DateTime.MinValue &&
                (now - lastAnnouncementTime).TotalSeconds < interval)
            {
                return;
            }

            int minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            int seconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
            string message = Util.FormatString(config.RestartAnnouncement,
                "{minutes}", minutes.ToString(),
                "{seconds}", seconds.ToString());

            Core.Rcon.Say(message);
            lastAnnouncementTime = now;
        }
    }
}
