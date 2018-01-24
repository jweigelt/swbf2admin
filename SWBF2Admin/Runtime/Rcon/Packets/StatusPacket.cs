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
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Rcon.Packets
{
    class StatusPacket : RconPacket
    {
        private ServerInfo info;
        public ServerInfo Info { get { return info; } }

        public StatusPacket() : base("status") { }

        /**
         * Server name : LeKeks test
         * Server IP   : 192.168.188.123
         * Version     : 1.00
         * Max players : 15
         * Password    : 
         * Current map : 
         * Next map    : 
         * Game mode   : CON
         * Players     : 0/0/0
         * Scores      : 0/0/0
         * Tickets     : 0/0
         * FF enabled  : yes
         * Heroes      : no
         **/

        public override void HandleResponse(string response)
        {
            string[] rows = response.Split('\n');
            if (rows.Length != 13) return;
            info = new ServerInfo();
            info.ServerName = GetVar(rows[0]);
            info.ServerIP = GetVar(rows[1]);
            info.Version = GetVar(rows[2]);
            info.MaxPlayers = GetVar(rows[3]);
            info.Password = GetVar(rows[4]);
            info.CurrentMap = GetVar(rows[5]);
            info.NextMap = GetVar(rows[6]);
            info.GameMode = GetVar(rows[7]);
            info.Players = GetVar(rows[8]);
            info.Scores = GetVar(rows[9]);
            info.Tickets = GetVar(rows[10]);
            info.FFEnabled = GetVar(rows[11]);
            info.Heroes = GetVar(rows[12]);
            PacketOk = true;
        }

        private string GetVar(string row)
        {
            return row.Substring(row.IndexOf(':') + 2);
        }
    }
}
 