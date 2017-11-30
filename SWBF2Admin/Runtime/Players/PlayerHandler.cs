using System.Collections.Generic;
using SWBF2Admin.Runtime.Rcon.Packets;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime
{
    class PlayerHandler : ComponentBase
    {
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

        public List<Player> GetPlayers_s()
        {
            return new List<Player>(playerList);
        }
    }
}