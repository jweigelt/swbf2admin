using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Commands.Misc
{
    public class CmdEndgame : ChatCommand
    {
        public CmdEndgame() : base("endgame", "endgame", "endgame") { }

        public override bool Run(Player player, string commandLine, string[] parameters)
        {
            Core.Rcon.SendCommand("endgame");
            return true;
        }
    }
}