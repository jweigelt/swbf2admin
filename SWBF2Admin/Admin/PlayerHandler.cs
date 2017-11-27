using System.Collections.Generic;
using SWBF2Admin.Rcon.Packets;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Admin
{
    class PlayerHandler
    {
        public virtual List<Player> PlayerList { get { return playerList; } }

        private List<Player> playerList;
        private AdminCore core;
        public PlayerHandler(AdminCore core)
        {
            this.core = core;
            playerList = new List<Player>();
        }

        public void Update()
        {
            PlayerListPacket plp = new PlayerListPacket();
            core.Rcon.SendPacket(plp);
            if(plp.PacketOk)
            {
                playerList = plp.PlayerList;
            }
        }

        public List<Player> GetPlayers_s()
        {
            return  new List<Player>(playerList);
        }
    }
}