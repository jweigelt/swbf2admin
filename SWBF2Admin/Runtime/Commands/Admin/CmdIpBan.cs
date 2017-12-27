using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    class CmdIpBan : CmdBan
    {
        public CmdIpBan() : base("ipban", "ban") { }
        public override void BanPlayer(Player p, Player admin, string reason = "")
        {
            PlayerBan b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, admin.Name, reason, BanType.IPAddress, p.DatabaseId, admin.DatabaseId);
            Core.Database.InsertBan(b);
        }
    }
}
