using System;
using System.IO;
using System.Text;


namespace SWBF2Admin.Maps.Lua
{
    struct LuaVMConfig
    {
        public byte
            littleEndian,
            intSz,
            sizeTSz,
            instrSz,
            opSz,
            opASz,
            opBSz,
            opCSz,
            luaNumSz;
    }

    enum LUA_T : byte
    {
        LUA_TNIL = 0,
        LUA_TNUMBER = 3,
        LUA_TSTRING = 4
    }


    class LuaVM
    {
        private byte[] LUA_SIGNATURE = new byte[] { 0x1b, 0x4c, 0x75, 0x61 };
        private const float LUA_TEST_NUM = 3.14159265358979323846E7f;

        private BinaryReader reader;
        private LuaVMConfig config;

        public LuaVM(Stream fs)
        {
            reader = new BinaryReader(fs);

            byte[] signature = reader.ReadBytes(LUA_SIGNATURE.Length);
            for (int i = 0; i < signature.Length; i++)
            {
                if (signature[i] != LUA_SIGNATURE[i]) throw new Exception("lua magic mismatch");
            }

            byte version = reader.ReadByte();
            if (version != 0x50) throw new Exception("lua version mismatch");

            config = ReadLuaVMConfig();
            //todo: validate remote vm config against local config

            if (ReadLuaNumber() != LUA_TEST_NUM)
            {
                throw new Exception("lua number mismatch");
            }


            LoadFunction();
        }

        private LuaVMConfig ReadLuaVMConfig()
        {
            LuaVMConfig config = new LuaVMConfig()
            {
                littleEndian = ReadByte(),
                intSz = ReadByte(),
                sizeTSz = ReadByte(),
                instrSz = ReadByte(),
                opSz = ReadByte(),
                //register sizes for A,B,C
                opASz = ReadByte(),
                opBSz = ReadByte(),
                opCSz = ReadByte(),
                luaNumSz = ReadByte(),
            };
            return config;
        }

        private LuaFunction LoadFunction()
        {
            LuaFunction def = new LuaFunction(
                LoadString(),
                ReadInt(),
                ReadByte(),
                ReadByte(),
                ReadByte(),
                ReadByte()
            );

            //lines
            int nLines = ReadInt();
            for (int i = 0; i < nLines; i++)
            {
                def.PushLine(ReadInt());
            }

            //locals
            int nLocals = ReadInt();
            for (int i = 0; i < nLocals; i++)
            {
                def.PushLocale(LoadString(), ReadInt(), ReadInt());
            }

            //upvalues
            int nUpvalues = ReadInt();
            for (int i = 0; i < nUpvalues; i++)
            {
                def.PushNup(LoadString());
            }

            //consants
            int nConst = ReadInt();
            for (int i = 0; i < nConst; i++)
            {
                LUA_T type = (LUA_T)ReadByte();
                switch (type)
                {
                    case LUA_T.LUA_TNUMBER:
                        def.PushConst(ReadLuaNumber());
                        break;
                    case LUA_T.LUA_TSTRING:
                        def.PushConst(LoadString());
                        break;
                    case LUA_T.LUA_TNIL:
                        def.PushConst();
                        break;
                    default:
                        throw new Exception("invalid const type");
                }
            }

            int nNested = ReadInt();
            for (int i = 0; i < nNested; i++)
            {
                def.PushNested(LoadFunction());
            }

            int nCode = ReadInt();
            for (int i = 0; i < nCode; i++)
            {
                def.PushCode(ReadBlock(config.instrSz));
            }

            return def;
        }

        private byte[] ReadBlock(int sz)
        {
            return reader.ReadBytes(sz);
        }

        private float ReadLuaNumber()
        {
            //lua 5.0 standard would use double instead of float
            byte[] number = ReadBlock(config.luaNumSz);
            return BitConverter.ToSingle(number, 0);
        }

        private byte ReadByte()
        {
            return ReadBlock(1)[0];
        }

        private int ReadSize()
        {
            byte[] sz = ReadBlock(config.sizeTSz);
            return BitConverter.ToInt32(sz, 0);
        }

        private int ReadInt()
        {
            byte[] sz = ReadBlock(config.intSz);
            return BitConverter.ToInt32(sz, 0);
        }
        private string LoadString()
        {
            int strLen = ReadSize();
            byte[] str = ReadBlock(strLen);
            return Encoding.ASCII.GetString(str);
        }
    }
}