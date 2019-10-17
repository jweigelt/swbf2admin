using System;

namespace SWBF2Admin.Maps.Lua
{
    enum LuaOpcode : byte
    {
        MOVE,
        LOADK,
        LOADBOOL,
        LOADNIL,
        GETUPVAL,
        GETGLOBAL,
        GETTABLE,
        SETGLOBAL,
        SETTABLE,
        NEWTABLE,
        SELF,
        ADD,
        SUB,
        MUL,
        DIV,
        POW,
        UNM,
        NOT,
        CONCAT,
        JMP,
        EQ,
        LT,
        LE,
        TEST,
        CALL,
        TAILCALL,
        RETURN,
        FORLOOP,
        TFORLOOP,
        TFORPREP,
        SETLIST,
        SETLISTO,
        CLOSE,
        CLOSURE
    }
    class LuaInstruction
    {
        public LuaOpcode OpCode { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }

        public LuaInstruction(byte[] instr)
        {
            OpCode = (LuaOpcode)extract(instr, 0, 6);
            A = extract(instr, 6, 8);
            C = 0;

            switch (OpCode)
            {
                //iABx
                case LuaOpcode.LOADK:
                case LuaOpcode.GETGLOBAL:
                case LuaOpcode.SETGLOBAL:
                case LuaOpcode.SETLIST:
                case LuaOpcode.SETLISTO:
                case LuaOpcode.CLOSURE:
                    B = extract(instr, 14, 18);
                    break;

                //iAsBx
                case LuaOpcode.JMP:
                case LuaOpcode.FORLOOP:
                case LuaOpcode.TFORPREP:
                    B = EvaluateLuaSignBit(extract(instr, 14, 18));
                    break;

                //iABC
                default:
                    C = EvaluateLuaSignBit(extract(instr, 14, 9));
                    B = EvaluateLuaSignBit(extract(instr, 23, 9));

                    break;
            }
            Console.WriteLine("{0}\tA:{1} B:{2} C:{3}", OpCode, A, B, C);
        }

        private static int EvaluateLuaSignBit(int i)
        {
            if ((i & (1 << 9)) != 0)
            {
                i ^= (1 << 9);
                i *= -1;
            }
            return i;
        }

        private int extract(byte[] instr, int pos, int sz)
        {
            int res = 0;
            int i = pos;
            int ctx = 0;

            while (i < pos + sz)
            {
                bool bit = (instr[i / 8] & (1 << (i % 8))) > 0;
                res |= ((int)(bit ? 1 : 0) << ctx);
                ctx++;
                i++;
            }
            return res;
        }
    }
}
