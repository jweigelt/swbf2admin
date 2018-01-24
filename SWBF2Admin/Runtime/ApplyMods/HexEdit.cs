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