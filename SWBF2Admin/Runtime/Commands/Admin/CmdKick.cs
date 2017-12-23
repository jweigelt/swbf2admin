using System;
using System.Xml.Serialization;
using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/kick.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdKick : PlayerCommand
    {

        public CmdKick() : base("kick", Permission.Kick) { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length > paramIdx)
            {
                string reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
            }
            //TODO: kick player
            throw new NotImplementedException();
        }
    }
}
