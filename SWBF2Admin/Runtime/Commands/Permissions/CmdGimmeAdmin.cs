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
            if (player.isSuperuser()) return false;
            return Core.Database.GroupEmpty(Core.Database.GetTopGroup());
        }
    }
}