using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWBF2Admin.Maps.Lua
{
    class LuaFunction
    {
        private List<LuaInstruction> luaAssembly = new List<LuaInstruction>();

        public LuaFunction(string name, int lineDefined, byte nups, byte numParams, byte variadic, byte maxStackSz)
        {
            Console.WriteLine("function: n:{0} l:{1} u:{2} p:{3} v:{4} s:{5}", name, lineDefined, nups, numParams, variadic, maxStackSz);
        }

        public void PushLine(int line)
        {
            Console.WriteLine("line: l:{0}", line);
        }

        public void PushLocale(string name, int startpc, int endpc)
        {
            Console.WriteLine("locale: n:{0} s:{1} e:{2} p:{3} v:{4} s:{5}", name, startpc, endpc);
        }

        public void PushNup(string name)
        {
            Console.WriteLine("nup: n:{0}", name);
        }

        public void PushConst()
        {
            Console.WriteLine("const: nil");
        }

        public void PushConst(string str)
        {
            Console.WriteLine("const: s:{0}", str);
        }

        public void PushConst(float f)
        {
            Console.WriteLine("const: f:{0}", f);
        }

        public void PushNested(LuaFunction f)
        {

        }

        public void PushCode(byte[] code)
        {
            luaAssembly.Add(new LuaInstruction(code));
        }
    }
}
