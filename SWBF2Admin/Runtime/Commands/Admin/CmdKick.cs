using System;
using System.Xml.Serialization;
using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    public class CmdKick : PlayerCommand
    {
        [XmlIgnore]
        public const string FILE_NAME = "./cfg/cmd/kick.xml";

        //[XmlIgnore]
        //public const string RESOURCE_NAME = "SWBF2Admin.Resources.cfg.cmd.kick.xml";

        public CmdKick() : base("kick", "kick") { }

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
