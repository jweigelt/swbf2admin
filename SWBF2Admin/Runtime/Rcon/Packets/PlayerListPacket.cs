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
            if (response.Length == 0) return; //no players

            string[] rows = response.Split('\n');

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