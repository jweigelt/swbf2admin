using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/ban.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.ban.xml"*/)]
    public class CmdBan : PlayerCommand
    {

        public string OnBan { get; set; } = "{player} was banned by {admin}";
        public string OnBanReason { get; set; } = "{player} was kicked by {admin} for {reason}";
        public CmdBan() : base("ban", Permission.Ban) { }
        public CmdBan(string n, Permission p) : base(n, p) { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length > paramIdx)
            {
                string reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
                SendFormatted(OnBanReason, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{reason}", reason);
            }
            else
            {
                SendFormatted(OnBan, "{player}", affectedPlayer.Name, "{admin}", player.Name);
            }

            Core.Players.Kick(affectedPlayer);
            return true;
        }

        public virtual void BanPlayer(Player p, Player admin, string reason = "")
        {
            PlayerBan b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, admin.Name, reason, BanType.Keyhash, p.DatabaseId, admin.DatabaseId);
            Core.Database.InsertBan(b);
        }
    }
}
