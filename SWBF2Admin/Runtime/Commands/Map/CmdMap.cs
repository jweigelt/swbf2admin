using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Map
{
    [ConfigFileInfo(fileName: "./cfg/cmd/map.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.addmap.xml"*/)]
    public class CmdMap : MapCommand
    {
        public string OnMapLoad { get; set; } = "Force-loading {map_nicename} ({map_name}{gamemode})";
        public CmdMap() : base("map", "map") { }

        public override bool AffectMap(ServerMap map, string mode, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            SendFormatted(OnMapLoad, "{map_name}", map.Name, "{map_nicename}", map.NiceName, "{gamemode}", mode);

            //hacky way of setting next map without having to worry about it being in the rotation
            Core.Rcon.SendCommand("removemap", map.Name + mode);
            Core.Rcon.SendCommand("addmap", map.Name + mode);
            Core.Rcon.SendCommand("endgame");
            return true;
        }
    }
}
