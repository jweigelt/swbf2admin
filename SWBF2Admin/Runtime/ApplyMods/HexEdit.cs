using System.IO;
using System.Globalization;
using System.Xml.Serialization;

using SWBF2Admin.Config;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.ApplyMods
{
    public class HexEdit
    {
        [XmlAttribute]
        public string FileName { get; set; }

        [XmlAttribute]
        public string StartAddress
        {
            get
            {
                return startAddress.ToString("X");
            }
            set
            {
                value = value.Replace("0x", "");
                startAddress = long.Parse(value, NumberStyles.HexNumber);
            }
        }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] PatchedBytes { get; set; }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] OriginalBytes { get; set; }

        [XmlAttribute]
        public string PatchedString
        {
            get { return Util.BytesToStr(PatchedBytes); }
            set { PatchedBytes = Util.StrToBytes(value); }
        }

        [XmlAttribute]
        public string OriginalString
        {
            get { return Util.BytesToStr(OriginalBytes); }
            set { OriginalBytes = Util.StrToBytes(value); }
        }

        private long startAddress;

        private void WriteHex(FileHandler io, byte[] buf, string levelDir)
        {
            using (FileStream stream = io.OpenStream($"{levelDir}/{FileName}"))
            {
                io.WriteBytes(stream, startAddress, buf);
            }
        }
         

        public void Revert(FileHandler io, string levelDir)
        {
            WriteHex(io, OriginalBytes, levelDir);
        }

        public void Apply(FileHandler io, string levelDir)
        {
            WriteHex(io, PatchedBytes, levelDir);
        }
    }
}