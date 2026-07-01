using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SWBF2Admin.Runtime.ProcessMods
{
    public class ProcessEdit
    {
        private int _moduleOffset;

        [XmlAttribute]
        public string ModuleOffset
        {
            get
            {
                return _moduleOffset.ToString("X");
            }
            set
            {
                value = value.Replace("0x", "");
                _moduleOffset = int.Parse(value, NumberStyles.HexNumber);
            }
        }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] PatchedBytes { get; set; }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] OriginalBytes { get; set; }

        public void Apply(ProcessMemoryReader reader)
        {
            reader.WriteBytes(reader.GetModuleBase(_moduleOffset), PatchedBytes);
        }
        public void Revert(ProcessMemoryReader reader)
        {
            reader.WriteBytes(reader.GetModuleBase(_moduleOffset), OriginalBytes);
        }
    }
}
