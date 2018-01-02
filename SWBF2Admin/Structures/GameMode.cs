namespace SWBF2Admin.Structures
{
    public sealed class GameMode
    {
        public static readonly GameMode ELI = new GameMode("eli");
        public static readonly GameMode ASS = new GameMode("ass");
        public static readonly GameMode HUNT = new GameMode("hunt");
        public static readonly GameMode CTF = new GameMode("ctf");
        public static readonly GameMode ONEFLAG = new GameMode("1flag");
        public static readonly GameMode CON = new GameMode("con");

        string mode;

        public GameMode(string mode)
        {
            this.mode = mode;
        }

        public static bool IsScoreMode(GameMode mode)
        {
            return (mode == CTF || mode == ONEFLAG);
        }


        public static GameMode GetModeByName(string name)
        {
            name = name.ToLower();
            switch (name)
            {
                case "ass":
                    return ASS;
                case "eli":
                    return ELI;
                case "1flag":
                    return ONEFLAG;
                case "ctf":
                    return CTF;
                case "hunt":
                    return HUNT;
                case "con":
                    return CON;
                default:
                    return null;
            }

        }

        public override string ToString()
        {
            return mode;
        }

    }
}
