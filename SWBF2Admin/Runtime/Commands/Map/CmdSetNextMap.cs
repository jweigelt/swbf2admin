/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
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