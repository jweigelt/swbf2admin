using MoonSharp.Interpreter;
using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SWBF2Admin.Runtime.ProcessMods
{
    [MoonSharpUserData]
    public class ProcessMod
    {

        [XmlAttribute]
        public string Name { get; set; } = "change me";

        [XmlAttribute]
        public bool ApplyOnStart { get; set; } = false;

        [XmlAttribute]
        public bool RevertOnStart { get; set; } = false;

        [MoonSharpHidden]
        public List<ProcessEdit> ProcessEdits { get; set; } = new List<ProcessEdit>();

        [MoonSharpHidden]
        public List<CodeCave> CodeCaves { get; set; } = new List<CodeCave>();

        [MoonSharpHidden]
        public void Apply(ProcessMemoryReader reader)
        {
            foreach (ProcessEdit edit in ProcessEdits)
            {
                edit.Apply(reader);
            }

            foreach (CodeCave codeCave in CodeCaves)
            {
                codeCave.CreateCodeCave(reader);
            }
        }
        [MoonSharpHidden]
        public void Revert(ProcessMemoryReader reader)
        {
            foreach (ProcessEdit edit in ProcessEdits)
            {
                edit.Revert(reader);
            }

            foreach (CodeCave cc in CodeCaves)
            {
                cc.RemoveCave(reader);
            }
        }
    }
}
