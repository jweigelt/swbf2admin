using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System;
using System.Reflection;

namespace SWBF2Admin.Structures.InGame
{
    public static class CharacterUtils
    {
        private static readonly int[] CHAR_OFFSET = { 0x1B0 };
        private static readonly int[] SCORE_OFFSET = { 0x1F8 };

        /// <summary>
        /// Gets the score of a character at the specified index.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <param name="reader">The ProcessMemoryReader instance.</param>
        /// <returns>The score of the character.</returns>
        public static Character GetCharacter(int index, ProcessMemoryReader reader)
        {
            IntPtr tablePtr = GetCharTableBase(reader);
            if (tablePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to retrieve the character table base address.");
            }

            IntPtr charPtr = IntPtr.Add(tablePtr, (index * CHAR_OFFSET[0]));
            Character character = new Character(charPtr, reader);
            return character;
        }

        /// <summary>
        /// Sets the score of a character at the specified index.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <param name="newScore">The new score value.</param>
        /// <param name="reader">The ProcessMemoryReader instance.</param>
        public static void SetScore(int index, int points, ProcessMemoryReader reader)
        {
            IntPtr scoreTablePtr = GetScoreTableBase(reader);
            if (scoreTablePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to retrieve the score table base address.");
            }

            IntPtr scorePtr = IntPtr.Add(scoreTablePtr, (index * SCORE_OFFSET[0]));
            SimpleScore score = new SimpleScore(scorePtr, reader);

            score.SetPoints(points);
        }

        /// <summary>
        /// Retrieves the base pointer for the score table from memory.
        /// </summary>
        /// <param name="reader">The ProcessMemoryReader instance.</param>
        /// <returns>The base pointer for the score table.</returns>
        private static IntPtr GetScoreTableBase(ProcessMemoryReader reader)
        {
            return reader.ReadPtr(reader.GetModuleBase(Offsets.CHAR_SCORE_OFFSET));
        }

        /// <summary>
        /// Retrieves the base pointer for the score table from memory.
        /// </summary>
        /// <param name="reader">The ProcessMemoryReader instance.</param>
        /// <returns>The base pointer for the score table.</returns>
        private static IntPtr GetCharTableBase(ProcessMemoryReader reader)
        {
            return reader.ReadPtr(reader.GetModuleBase(Offsets.CHAR_TABLE_OFFSET));
        }
    }
}
