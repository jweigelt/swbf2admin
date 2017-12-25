using System;
using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/addmap.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.addmap.xml"*/)]
    public class CmdAddMap : MapCommand
    {
        public string OnAddMap { get; set; } = "Map {map_nicename} ({map_name}{gamemode}) was added to the map rotation.";
        public CmdAddMap() : base("addmap", Permission.SetMap) { }

        public override bool AffectMap(ServerMap map, string mode, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            SendFormatted(OnAddMap, "{map_name}", map.Name, "{map_nicename}", map.NiceName, "{gamemode}", mode);
            return true;
        }
    }
}
