using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System.Globalization;
using System.Xml.Serialization;

namespace SWBF2Admin.Runtime.ProcessMods
{
    public class ProcessEdit
    {
        private long _moduleOffset;

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
                _moduleOffset = long.Parse(value, NumberStyles.HexNumber);
            }
        }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] PatchedBytes { get; set; }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] OriginalBytes { get; set; }

        public void Apply(ProcessMemoryReader reader)
        {
            reader.WriteBytes(reader.GetModuleBase(_moduleOffset), PatchedBytes);
            Logger.Log(LogLevel.Verbose, "Applied process edit at offset 0x{0} with bytes: {1}", _moduleOffset.ToString("X"), string.Join(" ", PatchedBytes.ToString()));
        }
        public void Revert(ProcessMemoryReader reader)
        {
            reader.WriteBytes(reader.GetModuleBase(_moduleOffset), OriginalBytes);
            Logger.Log(LogLevel.Verbose, "Reverted process edit at offset 0x{0} with bytes: {1}", _moduleOffset.ToString("X"), string.Join(" ", OriginalBytes.ToString()));
        }
    }
}
