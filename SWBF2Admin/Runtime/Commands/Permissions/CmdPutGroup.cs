using SWBF2Admin.Structures;
using SWBF2Admin.Config;
using SWBF2Admin.Runtime.Commands.Admin;

namespace SWBF2Admin.Runtime.Commands.Misc
{
    [ConfigFileInfo(fileName: "./cfg/cmd/putgroup.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdPutGroup : PlayerCommand
    {
        public string OnNoGroup { get; set; } = "No group specified. Usage: {usage}";
        public string OnInvalidGroup { get; set; } = "No group matching {group} could be found.";
        public string OnInvalidLevel { get; set; } = "You don't have permission to assign players to {group}.";
        public string OnAlreadyMember { get; set; } = "Player {player} is already a member of {group}";

        public string OnPutGroup { get; set; } = "Player {player} was added to group {group}.";
        public bool CheckLevel { get; set; } = true;

        public CmdPutGroup() : base("putgroup", "putgroup", "putgroup <player> [-n <num>] <group>") { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length <= paramIdx)
            {
                SendFormatted(OnNoGroup, "{usage}", Usage);
                return false;
            }

            PlayerGroup group = Core.Database.GetPlayerGroup(parameters[paramIdx]);
            if (group == null)
            {
                SendFormatted(OnInvalidGroup, "{group}", parameters[paramIdx]);
                return false;
            }

            //TODO
            if (CheckLevel && false)
            {
                SendFormatted(OnInvalidLevel, "{group}", group.Name);
                return false;
            }

            if (Core.Database.IsGroupMember(affectedPlayer, group))
            {
                SendFormatted(OnAlreadyMember, "{player}", affectedPlayer.Name, "{group}", group.Name);
                return false;
            }

            Core.Database.AddPlayerGroup(affectedPlayer, group);
            SendFormatted(OnPutGroup, "{player}", affectedPlayer.Name, "{group}", group.Name);
            return true;
        }
    }
}