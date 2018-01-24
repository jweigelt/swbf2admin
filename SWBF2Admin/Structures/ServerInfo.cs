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
using SWBF2Admin.Gameserver;
using System;
using MoonSharp.Interpreter;

namespace SWBF2Admin.Structures
{
    [MoonSharpUserData]
    public class ServerInfo
    {
        [MoonSharpHidden]
        public ServerStatus Status { get; set; }
        public virtual int StatusId { get { return (int)Status; } }

        public string ServerName { get; set; } = " - ";
        public string ServerIP { get; set; } = " - ";
        public string Version { get; set; } = " - ";
        public string MaxPlayers { get; set; } = " - ";
        public string Password { get; set; } = " - ";
        public string CurrentMap { get; set; } = " - ";
        public string NextMap { get; set; } = " - ";
        public string GameMode { get; set; } = " - ";
        public string Players { get; set; } = "0";
        public string Scores { get; set; } = "0/0/0";
        public String Tickets { get; set; } = "0/0";

        //TODO: not sure which values represent which team ...
        public virtual int Team1Score { get { return int.Parse(Scores.Split('/')[0]); } }
        public virtual int Team2Score { get { return int.Parse(Scores.Split('/')[1]); } }

        public virtual int Team1Tickets { get { return int.Parse(Tickets.Split('/')[0]); } }
        public virtual int Team2Tickets { get { return int.Parse(Tickets.Split('/')[1]); } }
  

        /* 
         * Trying to figure out tickets format for tat2g_eli
        private uint[] tickets = { 0, 0 };
        public string Tickets
        {
            get
            {
                return string.Format("{0}/{1}", tickets[0], tickets[1]);
            }
            set
            {
                string[] s = value.Split('/');
                tickets[0] = BitConverter.ToUInt32(BitConverter.GetBytes(int.Parse(s[0])), 0) + 180;
                tickets[1] = BitConverter.ToUInt32(BitConverter.GetBytes(int.Parse(s[1])), 0) + 180;
                tickets[0] -= Int32.MaxValue;
                tickets[1] -= Int32.MaxValue;

            }
        }*/

        public string FFEnabled { get; set; } = " - ";
        public string Heroes { get; set; } = " - ";

    }
}