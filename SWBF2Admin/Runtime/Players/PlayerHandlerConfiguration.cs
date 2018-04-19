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
using System.Collections.Generic;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Players
{
    /// <summary>
    /// Configuration class for player-related stuff
    /// (templates etc.)
    /// </summary>
    [ConfigFileInfo(fileName: "./cfg/players.xml")]
    public class PlayerHandlerConfiguration
    {

        public string OnPlayerAutoKickBanned { get; set; } = "Auto-kicking {player} - player banned.";

        /// <summary>
        ///Delay between /players requests
        /// </summary>
        public int PlayersUpdateInterval { get; set; } = 8000;

        public List<ConditionalMessage> ConditionalMessages { get; set; } = new List<ConditionalMessage>();

        public bool EnablePlayerStatsLogging { get; set; } = true;
    }
}