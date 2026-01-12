namespace SWBF2Admin.Structures.InGame
{
    public static class Offsets
    {
        public static readonly int CHAR_TABLE_OFFSET;
        public static readonly int CHAR_SCORE_OFFSET;

        static Offsets()
        {
            //TODO: Support for Aspyr and Spy? 

            // GoG
            CHAR_SCORE_OFFSET = 0x01A317D8;
            CHAR_TABLE_OFFSET = 0x01A317D4;
           
        }
    }
}
