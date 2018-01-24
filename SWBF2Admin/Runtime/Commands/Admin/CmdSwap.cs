/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/swap.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdSwap : PlayerCommand
    {

        public string OnSwap { get; set; } = "{player} was swapped by {admin}";
        public string OnSwapReason { get; set; } = "{player} was swapped by {admin} for {reason}";

        public CmdSwap() : base("swap", "swap") { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length > paramIdx)
            {
                string reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
                SendFormatted(OnSwapReason, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{reason}", reason);
            }
            else
            {
                SendFormatted(OnSwap, "{player}", affectedPlayer.Name, "{admin}", player.Name);
            }

            Core.Players.Swap(affectedPlayer);
            return true;
        }
    }
}