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
using System;
namespace SWBF2Admin.Structures
{
    public class GameInfo
    {
        private int team1Score, team2Score, team1Tickets, team2Tickets;
        public string Map { get; }
        public GameMode Mode { get; }
        public virtual int Team1Score { get { return team1Score; } }
        public virtual int Team2Score { get { return team2Score; } }
        public virtual int Team1Tickets { get { return team1Tickets; } }
        public virtual int Team2Tickets { get { return team2Tickets; } }
        public DateTime GameStarted { get; }
        public long DatabaseId { get; }

        public virtual byte WinningTeam
        {
            get
            {
                if (GameMode.IsScoreMode(Mode))
                {
                    if (Mode == GameMode.ASS) //quickfix for swbf2's faulty number formatting
                        return (Team1Score < Team2Score ? (byte)1 : (byte)0);
                    else
                        return (Team1Score > Team2Score ? (byte)1 : (byte)0);
                }

                else
                    return (Team1Tickets > Team2Tickets ? (byte)1 : (byte)0);
            }
        }

        public GameInfo(string map, string mode)
        {
            Mode = GameMode.GetModeByName(mode);
            Map = map;
        }

        public GameInfo(long databaseId, DateTime gameStarted, string map, string mode, int Team1Score, int Team2Score, int Team1Tickets, int Team2Tickets)
        {
            Map = map;
            Mode = GameMode.GetModeByName(mode);
            DatabaseId = databaseId;
            GameStarted = GameStarted;
        }

        public void UpdateScore(ServerInfo info)
        {
            team1Score = info.Team1Score;
            team2Score = info.Team2Score;
            team1Tickets = info.Team1Tickets;
            team2Tickets = info.Team2Tickets;
        }
    }
}
