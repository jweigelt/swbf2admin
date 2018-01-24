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
using System.Xml.Serialization;
using System.Collections.Generic;
using SWBF2Admin.Config;
using MoonSharp.Interpreter;

namespace SWBF2Admin.Runtime.ApplyMods
{
    [MoonSharpUserData]
    public class LvlMod
    {
        [XmlAttribute]
        public bool RevertOnStart { get; set; } = false;

        [XmlAttribute]
        public bool ApplyOnStart { get; set; } = false;

        [XmlAttribute]
        public string Name { get; set; } = "change me";

        [MoonSharpHidden]
        public List<HexEdit> HexEdits { get; set; } = new List<HexEdit>();

        [XmlIgnore]
        public virtual bool Active { get { return active; } }

        private bool active = false;

        [MoonSharpHidden]
        public void Revert(FileHandler io, string levelDir)
        {
            foreach (HexEdit he in HexEdits) he.Revert(io, levelDir);
            active = false;
        }

        [MoonSharpHidden]
        public void Apply(FileHandler io, string levelDir)
        {
            foreach (HexEdit he in HexEdits) he.Apply(io, levelDir);
            active = true;
        }
    }
}
