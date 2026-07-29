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
                _redirectOffset = long.Parse(value, NumberStyles.HexNumber);
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
                _customAddresses = value
                    .Split(',')
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => long.Parse(x, NumberStyles.HexNumber))
                    .ToList();
            }
        }

        [XmlIgnore]
        public IntPtr CaveAddress { get; private set; }
        private IntPtr JmpAddress;
        private long _redirectOffset;
        private byte[] _caveBytes;
        private List<long> _customAddresses;
        private bool isTarget64Bit;

        [MoonSharpHidden]
        public void CreateCodeCave(ProcessMemoryReader reader)
        {
            isTarget64Bit = reader.IsTarget64Bit;
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
            reader.WriteBytes(CaveAddress, _caveBytes);
            reader.WriteBytes(JmpAddress, jmpBytes);
        }
        [MoonSharpHidden]
        private byte[] GetJmpBytes()
        {
            return isTarget64Bit ? GetAbsJmpBytes(CaveAddress) : GetRelJmpBytes(CaveAddress);
        }

        [MoonSharpHidden]
        private byte[] GetJmpBackBytes()
        {
            long returnAddr = JmpAddress.ToInt64() + OriginalBytes.Length;
            return isTarget64Bit
                ? GetAbsJmpBytes(new IntPtr(returnAddr), padToOriginalLength: false)
                : GetRelJmpBackBytes(returnAddr);
        }

        [MoonSharpHidden]
        private byte[] GetRelJmpBackBytes(long returnAddr)
        {
            byte[] jmpBytes = new byte[]
            {
                0xE9, 0, 0, 0, 0
            };

            long endOfCave = CaveAddress.ToInt64() + _caveBytes.Length + 5;   //+5 = the E9 jmp appended after the cave
            int displacement = checked((int)(returnAddr - endOfCave));
            BitConverter.GetBytes(displacement).CopyTo(jmpBytes, 1);
            return jmpBytes;
        }

        [MoonSharpHidden]
        private byte[] GetRelJmpBytes(IntPtr dst)
        {
            byte[] jmpBytes = new byte[]
            {
                0xE9,0,0,0,0
            };

            //Calc relative offset from cave address to jmp address
            int displacement = checked((int)(dst.ToInt64() - (JmpAddress.ToInt64() + 5)));

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
        private byte[] GetAbsJmpBytes(IntPtr dst, bool padToOriginalLength = true)
        {
            byte[] jmpBytes = new byte[]
            {
                0x48, 0xB8, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xE0, // mov rax, dst
            };

            BitConverter.GetBytes((long)dst).CopyTo(jmpBytes, 2);

            return padToOriginalLength
                ? PadWithNops(jmpBytes, OriginalBytes.Length)
                : jmpBytes;
        }

        [MoonSharpHidden]
        private byte[] PadWithNops(byte[] bytes, int totalLength)
        {
            if (bytes.Length > totalLength)
            {
                throw new InvalidOperationException($"Detour needs {bytes.Length} bytes, but OriginalBytes only has {totalLength} bytes.");
            }

            if (bytes.Length == totalLength)
            {
                return bytes;
            }

            return bytes.Concat(Enumerable.Repeat((byte)0x90, totalLength - bytes.Length)).ToArray();
        }

        [MoonSharpHidden]
        public void RemoveCave(ProcessMemoryReader reader)
        {
            reader.WriteBytes(JmpAddress, OriginalBytes);
            reader.FreeMemory(CaveAddress);
            CaveAddress = IntPtr.Zero;
        }

        [MoonSharpHidden]
        public void ResetAllocation()
        {
            CaveAddress = IntPtr.Zero;
        }
        [MoonSharpHidden]
        private byte[] InjectCustomAddresses(ProcessMemoryReader reader)
        {
            // Create a copy so not overwriting xml file?
            string caveStr = CaveBytes;

            for (int i = 0; i < _customAddresses.Count; i++)
            {
                IntPtr address = reader.GetModuleBase(_customAddresses[i]);

                byte[] addressBytes = isTarget64Bit
                    ? BitConverter.GetBytes(address.ToInt64())
                    : BitConverter.GetBytes(address.ToInt32());

                // basically does the address backwards in str (little endian??? idk) and pads a 0 if it's smaller than 0xF
                string adressString = string.Join("", addressBytes.Select(x => x.ToString("X2")));

                caveStr = caveStr.Replace("{" + i + "}", adressString);
            }

            return Util.HexStrtoByteArray(caveStr);
        }
    }
}
