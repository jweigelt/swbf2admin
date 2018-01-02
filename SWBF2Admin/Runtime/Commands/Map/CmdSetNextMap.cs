using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Map
{
    [ConfigFileInfo(fileName: "./cfg/cmd/setnextmap.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.addmap.xml"*/)]
    public class CmdSetNextMap : MapCommand
    {
        public string OnSetMap { get; set; } = "Map {map_nicename} ({map_name}{gamemode}) will be the next map.";
        public CmdSetNextMap() : base("setnextmap", "setnextmap") { }

        public override bool AffectMap(ServerMap map, string mode, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            SendFormatted(OnSetMap, "{map_name}", map.Name, "{map_nicename}", map.NiceName, "{gamemode}", mode);

            //hacky way of setting next map without having to worry about it being in the rotation
            Core.Rcon.SendCommand("removemap", map.Name + mode);
            Core.Rcon.SendCommand("addmap", map.Name + mode);
            return true;
        }
    }
}