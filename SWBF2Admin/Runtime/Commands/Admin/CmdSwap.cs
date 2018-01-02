using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/swap.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdSwap : PlayerCommand
    {

        public string OnSwap { get; set; } = "{player} was swapped by {admin}";
        public string OnSwapReason { get; set; } = "{player} was swapped by {admin} for {reason}";

        public CmdSwap() : base("swap", "swap") { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length > paramIdx)
            {
                string reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
                SendFormatted(OnSwapReason, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{reason}", reason);
            }
            else
            {
                SendFormatted(OnSwap, "{player}", affectedPlayer.Name, "{admin}", player.Name);
            }

            Core.Players.Swap(affectedPlayer);
            return true;
        }
    }
}