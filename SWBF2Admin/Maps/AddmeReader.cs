using System;
using System.IO;
using System.Text;
using SWBF2Admin.Maps.Lua;

namespace SWBF2Admin.Maps
{
    class AddmeReader
    {
        private const string UCFB_HEADER = "ucfb";

        private BinaryReader reader;

        private uint size;
        private string name;
        private string info;
        private uint bodySize;

        public AddmeReader(Stream fs)
        {
            reader = new BinaryReader(fs);
            if (ReadChunkIdentifier() != UCFB_HEADER)
            {
                throw new Exception("File header mismatch");
            }
            size = reader.ReadUInt32();

            while (NextChunk()) ;
        }

        private bool NextChunk()
        {
            string ci = ReadChunkIdentifier();
            switch (ci)
            {
                case "scr_":
                    uint scr_ = reader.ReadUInt32(); //todo
                    break;
                case "NAME":
                    name = ReadString();
                    break;
                case "INFO":
                    info = ReadString();
                    break;
                case "BODY":
                    ReadBody();
                    break;
            }

            return true;
        }

        private void Align()
        {
            while (reader.BaseStream.Position % 4 != 0)
            {
                long toPad = reader.BaseStream.Position % 4;
                reader.BaseStream.Seek(toPad, SeekOrigin.Current);
            }
        }

        private string ReadString()
        {
            int len = (int)reader.ReadUInt32();
            string r = DecodeString(reader.ReadBytes(len));
            Align();
            return r;
        }

        private string ReadChunkIdentifier()
        {
            return DecodeString(reader.ReadBytes(4));
        }

        private string DecodeString(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }

        private void ReadBody()
        {
            bodySize = reader.ReadUInt32();
            var l = new LuaVM(reader.BaseStream);
        }

        ~AddmeReader()
        {
            reader.Close();
        }
    }
}