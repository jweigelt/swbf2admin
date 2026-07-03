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
namespace SWBF2Admin.Runtime.Watchdog
{
    [ConfigFileInfo(fileName: "./cfg/schedule.xml", template: "SWBF2Admin.Resources.cfg.schedule.xml")]
    public class ScheduleConfiguration
    {
        /// <summary>
        /// Enable restarting the server after it has been running for a given amount of time
        /// </summary>
        public bool EnableScheduledRestart { get; set; } = false;

        /// <summary>
        /// Time (in seconds) the server has to be running before it is restarted at the next map change
        /// </summary>
        public int RestartThreshold { get; set; } = 21600;

        /// <summary>
        /// Enable broadcasting a message before the scheduled restart
        /// </summary>
        public bool EnableRestartAnnouncement { get; set; } = true;

        /// <summary>
        /// Message broadcasted before the scheduled restart
        /// </summary>
        public string RestartAnnouncement { get; set; } = "The server will restart after the current map.";

        /// <summary>
        /// Time (in seconds) between restart announcements
        /// </summary>
        public int AnnouncementInterval { get; set; } = 300;

        /// <summary>
        /// Time (in milliseconds) between checks
        /// </summary>
        public int CheckInterval { get; set; } = 60000;
    }
}
