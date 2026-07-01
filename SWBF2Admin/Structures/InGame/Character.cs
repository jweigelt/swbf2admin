using Newtonsoft.Json;
using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWBF2Admin.Structures.InGame
{
    public class Character
    {
        private static readonly int[] NAME_OFFSET = { 0x30 };
        public static readonly int[] INDEX_OFFSET = { 0x130 };  //WILL BE 0xFFFFFFFF IF AI IS POPULATING CHARACTER
        private static readonly int[] TEAMID_OFFSET = { 0x134 };
        private static readonly int[] SOLDIER_CLASSID_OFFSET = { 0x13C };

        private int ScoreOffset;
        private IntPtr baseAddr;
        private ProcessMemoryReader reader;

        private string cachedName;
        private int cachedIndex;
        private int cachedTeamID;
        private int cachedClassID;
        private SimpleScore cachedScore;

        public Character(IntPtr basePtr, ProcessMemoryReader reader)
        {
            ScoreOffset = Offsets.CHAR_SCORE_OFFSET;
            this.reader = reader;
            baseAddr = basePtr;

            cachedName = reader.ReadWString(reader.GetOffsetIntPtr(baseAddr, NAME_OFFSET), 64);
            cachedIndex = reader.ReadInt32(reader.GetOffsetIntPtr(baseAddr, INDEX_OFFSET));
            cachedTeamID = reader.ReadInt32(reader.GetOffsetIntPtr(baseAddr, TEAMID_OFFSET));
            cachedClassID = reader.ReadInt32(reader.GetOffsetIntPtr(baseAddr, SOLDIER_CLASSID_OFFSET));

            // Scores are kept in a seperate table similar to characters
            // Offsets are 0x1F8 and need to use the pointer to score table
            cachedScore = CreateScore();
        }

        public void Update()
        {
            cachedName = reader.ReadWString(reader.GetOffsetIntPtr(baseAddr, NAME_OFFSET), 64);
            cachedIndex = reader.ReadInt32(reader.GetOffsetIntPtr(baseAddr, INDEX_OFFSET));
            cachedTeamID = reader.ReadInt32(reader.GetOffsetIntPtr(baseAddr, TEAMID_OFFSET));
            cachedClassID = reader.ReadInt32(reader.GetOffsetIntPtr(baseAddr, SOLDIER_CLASSID_OFFSET));
            cachedScore = CreateScore();
        }

        [JsonIgnore]
        public virtual bool Exists
        {
            get
            {
                return !baseAddr.Equals(IntPtr.Zero);
            }
        }
        public virtual bool IsHuman
        {
            get
            {
                return Index > -1;
            }
        }
        public virtual string Name
        {
            get
            {
                return cachedName;
            }

        }
        public virtual int Index
        {
            get
            {
                return cachedIndex;
            }
        }
        public int TeamID
        {
            get
            {
                return cachedTeamID;
            }
        }
        public int ClassID
        {
            get
            {
                return cachedClassID;
            }
        }
        private SimpleScore CreateScore()
        {
            if (cachedScore == null)
            {
                return new SimpleScore(IntPtr.Add(reader.ReadPtr(reader.GetModuleBase(ScoreOffset)), (Index) * 0x1F8), reader);
            }
            else
            {
                Score.Update();
                return Score;
            }
        }
        public SimpleScore Score
        {
            get
            {
                return cachedScore;
            }
        }
    }
}
