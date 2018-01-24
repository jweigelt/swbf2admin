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
using System.Collections.Generic;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    public abstract class PlayerCommand : ChatCommand
    {
        public string OnNoPlayerGiven { get; set; } = "No player specified. Usage: {usage}";
        public string OnNoPlayerFound { get; set; } = "No player matching {playerexp} could be found.";
        public string OnAmbiguous { get; set; } = "Multiple players matching {playerexp} found. Use -n (0-{matchcount})";
        public string OnInvalidNumber { get; set; } = "Invalid input {input}. Expecting valid integer.";
        public string OnInvalidIndex { get; set; } = "Invalid index. Please specify index between 0 and {matchcount}.";
        public string OptionNumber { get; set; } = "-n";

        public PlayerCommand(string alias, string permissionName) : base(alias, permissionName, $"{alias} <player> [-n <num>] [<reason>]") { }
        public PlayerCommand(string alias, string permissionName, string usage) : base(alias, permissionName, usage) { }
        public abstract bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx);
        public override bool Run(Player player, string commandLine, string[] parameters)
        {
            if (parameters.Length < 1)
            {
                SendFormatted(OnNoPlayerGiven, "{usage}", Usage);
                return false;
            }

            Player match = null;
            int parsed = 1;

            List<Player> matching = Core.Players.GetPlayers(parameters[0]);

            if (matching.Count == 0)
            {
                SendFormatted(OnNoPlayerFound, "{playerexp}", parameters[0]);
                return false;
            }

            else if (matching.Count > 1)
            {
                if ((parameters.Length > 1) && parameters[1].Equals(OptionNumber))
                {
                    if (parameters.Length < 3)
                    {
                        SendFormatted(OnNoPlayerGiven, "{usage}", Usage);
                        return false;
                    }

                    int i = 0;

                    if (!int.TryParse(parameters[2], out i))
                    {
                        SendFormatted(OnInvalidNumber, "{input}", parameters[2]);
                        return false;
                    }

                    if (i < 0 || i >= matching.Count)
                    {
                        SendFormatted(OnInvalidIndex, "{matchcount}", matching.Count.ToString());
                        return false;
                    }
                    match = matching[i];
                    parsed += 2;
                }
                else
                {
                    SendFormatted(OnAmbiguous, "{playerexp}", parameters[0], "{matchcount}", matching.Count.ToString());
                    return false;
                }
            }
            else
            {
                match = matching[0];
            }

            return AffectPlayer(match, player, commandLine, parameters, parsed);
        }
    }
}