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
namespace SWBF2Admin.Structures.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property)]

    class ConfigSection : Attribute
    {
        public const int GENERAL = 1 << 0;
        public const int GENERAL_KEEPDEFAULT = 1 << 1;
        public const int GAME = 1 << 2;
        public const int MAPS = 1 << 3;

        public int Type { get; set; }
        public virtual bool CanUpdate { get; }
        public virtual bool NeedsReload { get; }
        public bool YesNoBool { get; } //used for swbf's /command /nocommand parameters

        public ConfigSection(int type, bool canUpdate, bool needsReload)
        {
            Type = type;
            CanUpdate = canUpdate;
            NeedsReload = needsReload;
            YesNoBool = false;
        }

        public ConfigSection(int type)
        {
            Type = type;
            CanUpdate = NeedsReload = YesNoBool = false;
        }

        public ConfigSection(int type, bool yesNoBool)
        {
            Type = type;
            CanUpdate = NeedsReload = false;
            YesNoBool = yesNoBool;
        }
    }
}