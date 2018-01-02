using System;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/tempipban.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdTempIpBan : CmdTempban
    {
        public CmdTempIpBan() : base("tempipban", "tempipban") { }

        public override void BanPlayer(Player p, Player admin, TimeSpan duration, string reason = "")
        {
            PlayerBan b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, admin.Name, reason, duration, BanType.IPAddress, p.DatabaseId, admin.DatabaseId);
            Core.Database.InsertBan(b);
        }
    }
}