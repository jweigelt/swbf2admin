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

using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using MoonSharp.Interpreter;
using SWBF2Admin.Runtime.Players;
using SWBF2Admin.Structures.InGame;

namespace SWBF2Admin.Structures
{
    [MoonSharpUserData]
    public class Player
    {
        public byte Slot { get; }

        public ushort Ping { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Score { get; set; }
        public int Captures { get; set; }
        public int TeamKills { get; set; }
        public string Team { get; set; }
        public int TeamId { get; set; }

        public string Name { get; }
        public string KeyHash { get; }
        public bool IsBanned { get; set; }

        public int TotalVisits { get; set; }

        public Character Character { get; set; }

        [JsonIgnore]
        public PlayerGroup MainGroup { get; set; }

        public virtual string GroupName { get { return (MainGroup == null ? string.Empty : MainGroup.Name); } }

        [JsonIgnore]
        [MoonSharpHidden]
        public IPAddress RemoteAddress { get; }

        public virtual string RemoteAddressStr { get { return (RemoteAddress == null ? null : RemoteAddress.ToString());  } }

        public int DatabaseId { get; set; }

        [JsonIgnore]
        public static Player SUPERUSER = new Player(-1, 0, 0, 0, "superuser", "", "");

        [MoonSharpHidden]
        public Player(byte slot, ushort ping, int kills, int deaths, int score, string name, string keyhash, string team, IPAddress remoteAddress)
        {
            Slot = slot;
            Ping = ping;
            Kills = kills;
            Deaths = deaths;
            Score = score;
            Name = name;
            KeyHash = keyhash;
            Team = team;
            RemoteAddress = remoteAddress;
        }

        [MoonSharpHidden]
        public Player(int databaseId, int kills, int deaths, int score, string name, string keyhash, string team)
        {
            DatabaseId = databaseId;
            Kills = kills;
            Deaths = deaths;
            Score = score;
            Name = name;
            KeyHash = keyhash;
            Team = team;
        }

        [MoonSharpHidden]
        public Player(byte slot)
        {
            Slot = slot;
        }

        public bool isSuperuser()
        {
            return DatabaseId == SUPERUSER.DatabaseId &&
                   Name == SUPERUSER.Name;
        }

        #region Condition Tracker

        public void ResetConditionTracker()
        {
            KillsAtLastScore = Kills;
            KillsAtLastDeath = Kills;

            ScoreAtLastKill = Score;
            ScoreAtLastDeath = Score;

            DeathsAtLastScore = Deaths;
            DeathsAtLastKill = Deaths;
        }

        [MoonSharpHidden]
        public void ProcessPrevious(Player p)
        {
            DatabaseId = p.DatabaseId;
            TotalVisits = p.TotalVisits;
            IsBanned = p.IsBanned;
            MainGroup = p.MainGroup;
            MessageStates = p.MessageStates;

            //new game / new player -> we reset all conditional stats

            if (Deaths < p.Deaths)
            {
                HasSlotReset = true;
                ResetConditionTracker();
            }
            else
            {
                HasSlotReset = false;
            }


            if (Score > p.Score)
            {
                HasScored = true;
                KillsAtLastScore = Score;
                DeathsAtLastScore = Deaths;
            }
            else
            {
                KillsAtLastScore = p.KillsAtLastScore;
                DeathsAtLastScore = p.DeathsAtLastScore;
            }

            if (Kills > p.Kills)
            {
                HasKilled = true;
                ScoreAtLastKill = Score;
                DeathsAtLastKill = Deaths;
            }
            else
            {
                ScoreAtLastKill = p.ScoreAtLastKill;
                DeathsAtLastKill = p.DeathsAtLastKill;

            }

            if (Deaths > p.Deaths)
            {
                HasDied = true;
                KillsAtLastDeath = Kills;
                ScoreAtLastDeath = Score;
            }
            else
            {
                KillsAtLastDeath = p.KillsAtLastDeath;
                ScoreAtLastDeath = p.ScoreAtLastDeath;
            }
        }

        [JsonIgnore]
        public int ScoreAtLastDeath { get; set; } = 0;
        [JsonIgnore]
        public int ScoreAtLastKill { get; set; } = 0;

        [JsonIgnore]
        public int KillsAtLastDeath { get; set; } = 0;
        [JsonIgnore]
        public int KillsAtLastScore { get; set; } = 0;

        [JsonIgnore]
        public int DeathsAtLastKill { get; set; } = 0;
        [JsonIgnore]
        public int DeathsAtLastScore { get; set; } = 0;

        [JsonIgnore]
        public virtual int ScoreSinceLastKill
        {
            get
            {
                return Score - ScoreAtLastKill;
            }
        }
        [JsonIgnore]
        public virtual int ScoreSinceLastDeath
        {
            get
            {
                return Score - ScoreAtLastDeath;
            }
        }

        [JsonIgnore]
        public virtual int KillsSinceLastDeath
        {
            get
            {
                return Kills - KillsAtLastDeath;
            }
        }
        [JsonIgnore]
        public virtual int KillsSinceLastScore
        {
            get
            {
                return Kills - KillsAtLastScore;
            }
        }

        [JsonIgnore]
        public virtual int DeathsSinceLastScore
        {
            get
            {
                return Deaths - DeathsAtLastScore;
            }
        }
        [JsonIgnore]
        public virtual int DeathsSinceLastKill
        {
            get
            {
                return Deaths - DeathsAtLastKill;
            }
        }

        [JsonIgnore]
        public bool HasScored { get; set; } = false;
        [JsonIgnore]
        public bool HasKilled { get; set; } = false;
        [JsonIgnore]
        public bool HasDied { get; set; } = false;
        [JsonIgnore]
        public bool HasSlotReset { get; set; } = true;

        [JsonIgnore]
        public Dictionary<ConditionalMessage, bool> MessageStates;



        #endregion
    }
}