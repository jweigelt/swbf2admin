using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SWBF2Admin.Structures.InGame
{
    public class SimpleScore
    {
        #region Offsets
        //4Bytes
        private enum KillsOffsets : int
        {
            Team1 = 0x4,
            Team2 = 0x8
        }
        //2Bytes
        private enum PointsOffsets : int
        {
            All = 0x1F0,
            AllIndicator = 0x1F4
        }
        //2Bytes
        private enum TKOffsets : int
        {
            Team1 = 0xC,
            Team2 = 0xE
        }
        //2Bytes
        private enum DeathsOffsets : int
        {
            Team1 = 0x5A,
            Team2 = 0x5C
        }
        //2Bytes
        private enum FlagCapsOffsets : int
        {
            Team1 = 0x14,
            Team2 = 0x16
        }
        #endregion

        private int cachedPoints;
        private int cachedKills;
        private int cachedDeaths;
        private int cachedFlagCaps;
        private int cachedTeamKills;
        private int cachedTotalKills;

        private IntPtr baseAddr;
        private ProcessMemoryReader reader;

        public SimpleScore(IntPtr basePtr, ProcessMemoryReader reader)
        {
            this.reader = reader;
            baseAddr = basePtr;

            cachedPoints = CalcPoints();
            cachedKills = CalcKills();
            cachedDeaths = CalcDeaths();
            cachedFlagCaps = CalcFlagCaps();
            cachedTeamKills = CalcTeamKills();
            cachedTotalKills = CalcTotalKills();
        }

        public void Update()
        {
            cachedPoints = CalcPoints();
            cachedKills = CalcKills();
            cachedDeaths = CalcDeaths();
            cachedFlagCaps = CalcFlagCaps();
            cachedTeamKills = CalcTeamKills();
            cachedTotalKills = CalcTotalKills();
        }

        #region Properties
        [JsonIgnore]
        public virtual bool Exists
        {
            get
            {
                return !baseAddr.Equals(IntPtr.Zero);
            }
        }
        public virtual int Points
        {
            get
            {
                return cachedPoints;
            }
            set
            {
                reader.WriteInt32(IntPtr.Add(baseAddr, (int)PointsOffsets.All), value);
            }
        }
        
        public virtual int InGameKills
        {
            get
            {
                return cachedKills;
            }
        }

        public virtual int TotalKills
        {
            get
            {
                return cachedTotalKills;
            }
        }
        public virtual int Deaths
        {
            get
            {
                return cachedDeaths;
            }
        }
        public virtual int Captures
        {
            get
            {
                return cachedFlagCaps;
            }
        }
        public virtual int TeamKills
        {
            get
            {
                return cachedTeamKills;
            }
        }
        #endregion

        #region setters
        public void SetPoints(int value)
        {
            reader.WriteInt16(IntPtr.Add(baseAddr, (int)PointsOffsets.All), value);
        }
        public void SetKills(int teamId, int value)
        {
            int offset = teamId == 1 ? (int)KillsOffsets.Team1 : (int)KillsOffsets.Team2;
            reader.WriteInt32(IntPtr.Add(baseAddr, offset), value);
        }
        public void SetDeaths(int teamId, int value)
        {
            int offset = teamId == 1 ? (int)DeathsOffsets.Team1 : (int)DeathsOffsets.Team2;
            reader.WriteInt16(IntPtr.Add(baseAddr, offset), value);
        }
        public void SetFlagCaps(int teamId, int value)
        {
            int offset = teamId == 1 ? (int)FlagCapsOffsets.Team1 : (int)FlagCapsOffsets.Team2;
            reader.WriteInt16(IntPtr.Add(baseAddr, offset), value);
        }
        public void SetTeamKills(int teamId, int value)
        {
            int offset = teamId == 1 ? (int)TKOffsets.Team1 : (int)TKOffsets.Team2;
            reader.WriteInt16(IntPtr.Add(baseAddr, offset), value);
        }
        #endregion

        #region CalcProperties
        private int CalcPoints()
        {
            int amt = reader.ReadInt16(IntPtr.Add(baseAddr, (int)PointsOffsets.All));
            return amt;
        }
        private int CalcTotalKills()
        {
            int team1 = reader.ReadInt32(IntPtr.Add(baseAddr, (int)KillsOffsets.Team1));
            int team2 = reader.ReadInt32(IntPtr.Add(baseAddr, (int)KillsOffsets.Team2));
            return team1 + team2;
        }
        private int CalcKills()
        {
            int team1 = reader.ReadInt32(IntPtr.Add(baseAddr, (int)KillsOffsets.Team1));
            int team2 = reader.ReadInt32(IntPtr.Add(baseAddr, (int)KillsOffsets.Team2));
            int team1TK = reader.ReadInt16(IntPtr.Add(baseAddr, (int)TKOffsets.Team1));
            int team2TK = reader.ReadInt16(IntPtr.Add(baseAddr, (int)TKOffsets.Team2));
            return team1 + team2 - 2 * (team1TK + team2TK);
        }
        private int CalcDeaths()
        {
            int team1 = reader.ReadInt16(IntPtr.Add(baseAddr, (int)DeathsOffsets.Team1));
            int team2 = reader.ReadInt16(IntPtr.Add(baseAddr, (int)DeathsOffsets.Team2));
            return team1 + team2;
        }
        private int CalcFlagCaps()
        {
            int team1 = reader.ReadInt16(IntPtr.Add(baseAddr, (int)FlagCapsOffsets.Team1));
            int team2 = reader.ReadInt16(IntPtr.Add(baseAddr, (int)FlagCapsOffsets.Team2));
            return team1 + team2;
        }
        private int CalcTeamKills()
        {
            int team1 = reader.ReadInt16(IntPtr.Add(baseAddr, (int)TKOffsets.Team1));
            int team2 = reader.ReadInt16(IntPtr.Add(baseAddr, (int)TKOffsets.Team2));
            return team1 + team2;
        }
        #endregion
    }
}
