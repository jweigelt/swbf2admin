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

        public ServerInfo Info
        {
            get { return info; }
        }

        public StatusPacket() : base("status")
        {
        }

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
        /**
         * Server name : LeKeks test
         * Server IP   : 192.168.188.93
         * Version     : 1.00
         *  Max players : 35
         *  Current map : dea1c_con
         *  Next map    : dea1g_con
         *  Game mode   : CON
         *  Players     : 1/0/1
         *  Scores      : 0/0/0
         *  Tickets     : 450/450
         *  FF enabled  : no
         *  Heroes      : no
         */
        public override void HandleResponse(string response)
        {
            string[] rows = response.Split('\n');
            if (rows.Length != 13 && rows.Length != 12) return;
            int idx = 0;
            info = new ServerInfo();
            info.ServerName = GetVar(rows[idx++]);
            info.ServerIP = GetVar(rows[idx++]);
            info.Version = GetVar(rows[idx++]);
            info.MaxPlayers = GetVar(rows[idx++]);
            if (rows.Length == 13)
            {
                info.Password = GetVar(rows[idx++]);
            }
            info.CurrentMap = GetVar(rows[idx++]);
            info.NextMap = GetVar(rows[idx++]);
            info.GameMode = GetVar(rows[idx++]);
            info.Players = GetVar(rows[idx++]);
            info.Scores = GetVar(rows[idx++]);
            info.Tickets = GetVar(rows[idx++]);
            info.FFEnabled = GetVar(rows[idx++]);
            info.Heroes = GetVar(rows[idx]);
            PacketOk = true;
        }

        private string GetVar(string row)
        {
            return row.Substring(row.IndexOf(':') + 2);
        }
    }
}