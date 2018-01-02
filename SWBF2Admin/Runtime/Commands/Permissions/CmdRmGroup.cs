using SWBF2Admin.Structures;
using SWBF2Admin.Config;
using SWBF2Admin.Runtime.Commands.Admin;

namespace SWBF2Admin.Runtime.Commands.Misc
{
    [ConfigFileInfo(fileName: "./cfg/cmd/rmgroup.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdRmGroup : PlayerCommand
    {
        public string OnNoGroup { get; set; } = "No group specified. Usage: {usage}";
        public string OnInvalidGroup { get; set; } = "No group matching {group} could be found.";
        public string OnInvalidLevel { get; set; } = "You don't have permission to remove players from {group}.";
        public string OnNoMember { get; set; } = "Player {player} is not a member of {group}";

        public string OnPutGroup { get; set; } = "Player {player} was removed from {group}.";
        public bool CheckLevel { get; set; } = true;

        public CmdRmGroup() : base("rmgroup", "rmgroup", "rmgroup <player> [-n <num>] <group>") { }

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

            if (CheckLevel && (player.MainGroup == null || (player.MainGroup.Level <= group.Level)))
            {
                SendFormatted(OnInvalidLevel, "{group}", group.Name);
                return false;
            }

            if (!Core.Database.IsGroupMember(affectedPlayer, group))
            {
                SendFormatted(OnNoMember, "{player}", affectedPlayer.Name, "{group}", group.Name);
                return false;
            }

            Core.Database.RemovePlayerGroup(player, group);
            SendFormatted(OnPutGroup, "{player}", affectedPlayer.Name, "{group}", group.Name);
            return true;
        }
    }
}