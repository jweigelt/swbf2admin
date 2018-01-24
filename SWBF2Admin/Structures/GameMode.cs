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
                case "ctf1":
                    return ONEFLAG;
                case "ctf":
                    return CTF;         
                case "ctf2":
                    return CTF;
                case "hunt":
                    return HUNT;
                case "hun":
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
