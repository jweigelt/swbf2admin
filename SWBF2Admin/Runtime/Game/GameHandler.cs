using System;
using SWBF2Admin.Config;
using SWBF2Admin.Structures;
using SWBF2Admin.Runtime.Rcon.Packets;
namespace SWBF2Admin.Runtime.Game
{
    public class GameHandler : ComponentBase
    {
        private ServerInfo latestInfo;
        public ServerInfo LatestInfo { get { return latestInfo; } }

        GameHandlerConfiguration config;

        public GameHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<GameHandlerConfiguration>();
            UpdateInterval = this.config.StatusUpdateInterval;
        }

        public override void OnInit()
        {
            Core.Rcon.GameEnded += new EventHandler(Rcon_GameEnded);
        }

        public override void OnServerStart()
        {
            EnableUpdates();
            OnUpdate(); //make sure we get the first update fast
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            StatusPacket sp = new StatusPacket();
            Core.Rcon.SendPacket(sp);
            if (sp.PacketOk)
            {
                latestInfo = sp.Info;
            }
        }

        private void Rcon_GameEnded(object sender, EventArgs e)
        {

        }
    }
}