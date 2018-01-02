using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Permissions
{
    [ConfigFileInfo(fileName: "./cfg/cmd/gimmeadmin.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdGimmeAdmin : ChatCommand
    {
        public string OnPutGroup { get; set; } = "Player {player} was added to group {group}.";
        public string OnNoGroup { get; set; } = "No groups found.";
        public bool CheckLevel { get; set; } = true;

        public CmdGimmeAdmin() : base("gimmeadmin", "gimmeadmin", "gimmeadmin") { }

        public override bool Run(Player player, string commandLine, string[] parameters)
        {

            PlayerGroup group = Core.Database.GetTopGroup();
            if (group == null)
            {
                SendFormatted(OnNoGroup);
                return false;
            }

            Core.Database.AddPlayerGroup(player, group);
            SendFormatted(OnPutGroup, "{player}", player.Name, "{group}", group.Name);
            return true;
        }

        public override bool HasPermission(Player player)
        {
            return Core.Database.NoPlayerGroups();
        }
    }
}