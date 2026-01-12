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
using MoonSharp.Interpreter;
namespace SWBF2Admin.Structures
{
    [MoonSharpUserData]
    public class PlayerStatistics
    {
        public PlayerStatistics(int gamesWon, int gamesLost, int gamesQuit, int totalKills, int totalDeaths, int totalScore)
        {
            GamesWon = gamesWon;
            GamesLost = gamesLost;
            GamesQuit = gamesQuit;
            TotalKills = totalKills;
            TotalDeaths = totalDeaths;
            TotalScore = totalScore;
        }

        public PlayerStatistics() { }

        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int GamesQuit { get; set; }
        public virtual int TotalGames { get { return (GamesWon + GamesLost + GamesQuit); } }

        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalScore { get; set; }
        public int TotalCaptures { get; set; }
        public int TotalTeamKills { get; set; }
        public int TeamId { get; set; }

        public virtual float TotalKDRatio { get { return (float)TotalKills / TotalDeaths; } }
        public virtual float TotalKGRatio { get { return (float)TotalKills / TotalGames; } }
        public virtual float TotalSGRatio { get { return (float)TotalScore / TotalGames; } }
        public virtual float TotalDGRatio { get { return (float)TotalDeaths / TotalGames; } }

    }
}