using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Map
{
    [ConfigFileInfo(fileName: "./cfg/cmd/removemap.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.addmap.xml"*/)]
    public class CmdRemoveMap : MapCommand
    {
        public string OnNotInMapRot { get; set; } = "Map {map_nicename} ({map_name}{gamemode}) is not contained in the map rotation.";
        public string OnRemoveMap { get; set; } = "Map {map_nicename} ({map_name}{gamemode}) was removed from the map rotation.";
        public CmdRemoveMap() : base("removemap", "removemap") { }

        public override bool AffectMap(ServerMap map, string mode, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            string r = Core.Rcon.SendCommand("removemap", map.Name + mode);

            //TODO: check if that's what the server outputs
            if (r.Equals("map removed"))
                SendFormatted(OnRemoveMap, "{map_name}", map.Name, "{map_nicename}", map.NiceName, "{gamemode}", mode);
            else
                SendFormatted(OnNotInMapRot, "{map_name}", map.Name, "{map_nicename}", map.NiceName, "{gamemode}", mode);

            return true;
        }
    }
}
