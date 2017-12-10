using System.Collections.Generic;
using SWBF2Admin.Runtime.Rcon.Packets;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime
{
    class PlayerHandler : ComponentBase
    {
        /// <summary>List containing all connected players</summary>
        public virtual List<Player> PlayerList { get { return playerList; } }
        private List<Player> playerList;

        public PlayerHandler(AdminCore core) : base(core)
        {
            playerList = new List<Player>();
            UpdateInterval = 10000;
        }
        public override void OnServerStart()
        {
            EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        public override void OnUpdate()
        {
            PlayerListPacket plp = new PlayerListPacket();
            Core.Rcon.SendPacket(plp);
            if (plp.PacketOk)
            {
                playerList = plp.PlayerList;
            }
        }

        /// <summary>Gets a copy of the current playerlist</summary>
        public List<Player> GetPlayers_s()
        {
            return new List<Player>(playerList);
        }

        public List<Player> GetPlayers(string exp, bool ignoreCase = true, bool exact = false)
        {
            List<Player> matching = new List<Player>();

            if (playerList != null)
            {
                foreach (Player p in playerList)
                {
                    if ((!exact && ignoreCase && p.Name.ToLower().Contains(exp.ToLower())) ||
                        (!exact && p.Name.Contains(exp)) ||
                        (p.Name.Equals(exp)))
                    {
                        matching.Add(p);
                    }
                }
            }
            return matching;
        }

    }
}