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
using System;
using System.Net;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Rcon.Packets
{
    class PlayerListPacket : RconPacket
    {
        public List<Player> PlayerList { get; set; }

        public PlayerListPacket() : base("players") { }

        public override void HandleResponse(string response)
        {
            PlayerList = new List<Player>();
            if (response.Length == 0)
            {
                //no players
                PacketOk = true;
                return;
            }

            List<string> rows = new List<string>();

            for (int i = 0; i < response.Length; i+= 90)
            {
                if (response.Length >= i + 89)
                rows.Add(response.Substring(i, 89));
            }

            foreach (string r in rows)
            {
                try
                {
                    IPAddress ip;
                    if (!IPAddress.TryParse(r.Substring(42, 15).TrimEnd(), out ip))
                    {
                        //NOTE: GoG's Battlefront returns weird ips
                        ip = IPAddress.Parse("127.0.0.1");
                    }

                    string name = r.Substring(3, 17).TrimEnd();
                    name = name.Substring(1, name.Length - 2);

                    Player p = new Player(
                           byte.Parse(r.Substring(0, 2).TrimEnd()),  //Slot
                           ushort.Parse(r.Substring(37, 4).TrimEnd()),
                           int.Parse(r.Substring(29, 3).TrimEnd()),
                           int.Parse(r.Substring(33, 3).TrimEnd()),
                           int.Parse(r.Substring(25, 3).TrimEnd()),
                           name,
                           r.Substring(57, 32).TrimEnd(),
                           r.Substring(21, 3).TrimEnd(),
                           ip);

                    PlayerList.Add(p);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, "Failed to parse player '{0}' ({1})", r, e.ToString());
                }
            }
            PacketOk = true;
        }
    }
}