using MoonSharp.Interpreter;
using SWBF2Admin.Runtime.Readers;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace SWBF2Admin.Runtime.ProcessMods
{
    public class CodeCave
    {
        public string ToStr { get { return string.Format("JmpAddress: {0}\nCaveAddress: {1}\nCaveBytes: {2}", JmpAddress.ToString("X"), CaveAddress.ToString("X"), string.Join(" ", _caveBytes.Select(x => x.ToString("X")))); } }
        
        [XmlAttribute]
        public int MemoryAllocatedSize { get; set; }

        [XmlAttribute]
        public string RedirectModuleOffset { 
            get
            {
                return _redirectOffset.ToString("X");
            }
            set
            {
                value = value.Replace("0x", "");
                _redirectOffset = int.Parse(value, NumberStyles.HexNumber);
            }
        }

        [XmlAttribute(DataType = "hexBinary")]
        public byte[] OriginalBytes { get; set; }

        [XmlAttribute]
        public string CaveBytes { get; set; }

        [XmlAttribute]
        public string CustomAddresses { 
            get 
            {
                return string.Join(",", _customAddresses);
            }
            set 
            { 
                value = value.Replace("0x", ""); 
                _customAddresses = value.Split(',').Select(x => int.Parse(x, NumberStyles.HexNumber)).ToList();
            }
        }

        private IntPtr CaveAddress;
        private IntPtr JmpAddress;
        private int _redirectOffset;
        private byte[] _caveBytes;
        private List<int> _customAddresses;

        [MoonSharpHidden]
        public void CreateCodeCave(ProcessMemoryReader reader)
        {
            CaveAddress = reader.AllocateMemory(MemoryAllocatedSize);
            JmpAddress = reader.GetModuleBase(_redirectOffset);

            // Gets the op code to jmp to code cave
            byte[] jmpBytes = GetJmpBytes();

            // Format CaveBytes with custom addresses
            _caveBytes = InjectCustomAddresses(reader);

            // Gets the op code to jmp back to process 
            byte[] jmpBackBytes = GetJmpBackBytes();

            // Concat the jmp back op code to cave bytes
            _caveBytes = _caveBytes.Concat(jmpBackBytes).ToArray();

            // Write to memory
            reader.WriteBytes(JmpAddress, jmpBytes);
            reader.WriteBytes(CaveAddress, _caveBytes);
        }
        [MoonSharpHidden]
        private byte[] GetJmpBackBytes()
        {
            byte[] jmpByte = new byte[]
            {
                0xE9, 0, 0, 0, 0
            };
            int endOfCodeCave = (int)CaveAddress + _caveBytes.Length;
            int endOfOverwrite = (int)JmpAddress + (OriginalBytes.Length - 5);
            int displacement = endOfOverwrite - endOfCodeCave;
            BitConverter.GetBytes(displacement).CopyTo(jmpByte, 1);
            return jmpByte;
        }
        [MoonSharpHidden]
        private byte[] GetJmpBytes()
        {
            byte[] jmpBytes = new byte[]
            {
                0xE9,0,0,0,0
            };

            //Calc relative offset from cave address to jmp address
            int displacement = (int)CaveAddress - ((int)JmpAddress + 5);

            // Put the displacement in bytes into jmp bytes ( jmp 0x12345678 ) 
            BitConverter.GetBytes(displacement).CopyTo(jmpBytes, 1);

            // Need to fill the rest of the op code with NOPs to preserve functionale
            int remainingBytes = OriginalBytes.Length - jmpBytes.Length;
            if (remainingBytes > 0)
            {
                byte[] nopByte = new byte[remainingBytes];
                for(int i = 0; i < remainingBytes; i++)
                {
                    nopByte[i] = 0x90;
                }

                jmpBytes = jmpBytes.Concat(nopByte).ToArray();
            }
            return jmpBytes;
        }
        [MoonSharpHidden]
        public void RemoveCave(ProcessMemoryReader reader)
        {
            reader.WriteBytes(JmpAddress, OriginalBytes);
            reader.FreeMemory(CaveAddress);
        }
        [MoonSharpHidden]
        private byte[] InjectCustomAddresses(ProcessMemoryReader reader)
        {
            // Create a copy so not overwriting xml file?
            string caveStr = CaveBytes;

            for (int i = 0; i < _customAddresses.Count; i++)
            {
                IntPtr address = reader.GetModuleBase(_customAddresses[i]);

                byte[] addressBytes = BitConverter.GetBytes((int)address);
                // basically does the address backwards in str (little endian??? idk) and pads a 0 if it's smaller than 0xF
                string adressString = string.Join("", addressBytes.Select(x => x > 0xF ? x.ToString("X") : "0"+x.ToString("X")));

                string placeholder = "{" + i + "}";

                caveStr = caveStr.Replace(placeholder, adressString);
            }
            return Util.HexStrtoByteArray(caveStr);
        }
    }
}
