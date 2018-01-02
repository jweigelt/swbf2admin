using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/kick.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdKick : PlayerCommand
    {

        public string OnKick { get; set; } = "{player} was kicked by {admin}";
        public string OnKickReason { get; set; } = "{player} was kicked by {admin} for {reason}";

        public CmdKick() : base("kick", "kick") { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length > paramIdx)
            {
                string reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
                SendFormatted(OnKickReason, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{reason}", reason);
            } else
            {
                SendFormatted(OnKick, "{player}", affectedPlayer.Name, "{admin}", player.Name);
            }

            Core.Players.Kick(affectedPlayer);
            return true;
        }
    }
}