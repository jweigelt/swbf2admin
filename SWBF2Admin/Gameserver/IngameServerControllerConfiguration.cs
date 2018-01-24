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
using System.Net;
using System.Xml.Serialization;
using SWBF2Admin.Config;

namespace SWBF2Admin.Gameserver
{
    [ConfigFileInfo(fileName: "./cfg/ingameservercontroller.xml"/*,template: "SWBF2Admin.Resources.cfg.announce.xml"*/)]
    public class IngameServerControllerConfiguration
    {
        public int TcpTimeout { get; set; } = 100;
        public int StartupTime { get; set; } = 30000;
        public int NotRespondingCheckInterval { get; set; } = 5000;
        public int NotRespondingMaxCount { get; set; } = 2;
        public int ReadTimeout { get; set; } = 100;
        public int MapHangTimeout { get; set; } = 20000;
        public int FreezeTime { get; set; } = 1000;
        public int FreezesBeforeKill { get; set; } = 10;

        public string ServerHostname { get; set; } = "127.0.0.1:1138";

        [XmlIgnore]
        public virtual IPEndPoint ServerIPEP
        {
            get
            {
                string[] cc = ServerHostname.Split(':');
                return new IPEndPoint(IPAddress.Parse(cc[0]), (cc.Length > 1 ? int.Parse(cc[1]) : 4658));
            }
        }
    }
}
