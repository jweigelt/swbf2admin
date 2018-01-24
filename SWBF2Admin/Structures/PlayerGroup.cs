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
    public class PlayerGroup
    {
        public long Id { get; }
        public long Level { get; }
        public string Name { get; }
        public string WelcomeMessage { get; }
        public bool EnableWelcome { get; }

        public PlayerGroup(long id, long level, string name, string welcomeMessage, bool enableWelcome)
        {
            Id = id;
            Level = level;
            Name = name;
            WelcomeMessage = welcomeMessage;
            EnableWelcome = enableWelcome;
        }
    }
}