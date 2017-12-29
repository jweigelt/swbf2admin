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
