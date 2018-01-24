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
using System.Collections.Generic;
using SWBF2Admin.Config;
namespace SWBF2Admin.Runtime.Announce
{
    [ConfigFileInfo(fileName: "./cfg/announce.xml"/*,template: "SWBF2Admin.Resources.cfg.announce.xml"*/)]
    public class AnnounceHandlerConfiguration
    {
        /// <summary>
        /// Enable announce broadcasting
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Delay (in seconds) between broadcasts
        /// </summary>
        public int Interval { get; set; } = 360;

        /// <summary>
        /// List containing all announces
        /// </summary>
        public List<Announce> AnnounceList = new List<Announce>();
    }
}