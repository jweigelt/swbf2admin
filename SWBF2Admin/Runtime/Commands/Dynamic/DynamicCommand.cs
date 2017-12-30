using System;
using System.IO;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;

using MoonSharp.Interpreter;

namespace SWBF2Admin.Runtime.Commands.Dynamic
{
    public class DynamicCommand : ChatCommand
    {
        public object UserConfig { get; set; }

        private Script script = new Script();
        private string name;
        private LuaApi api;

        public void Init(AdminCore core, DirectoryInfo dir)
        {
            Core = core;
            api = new LuaApi(core, this);
            name = dir.Name;
            try
            {
                string lua = Core.Files.ReadFileText($"{dir.FullName}/{dir.Name}.lua");
                script.DoString(lua);
                script.Globals[LuaApi.GLOBALS_API] = api;
                CallFunction(LuaApi.FUNC_INIT);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to load {0}.lua : {1}. Disabling {0}.", dir.Name, e.Message);
                Enabled = false;
            }
        }

        public override bool Run(Player player, string commandLine, string[] parameters)
        {
            try
            {
                return CallFunction(LuaApi.FUNC_RUN,player, commandLine, parameters).Boolean;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to run {0} : {1}", name, e.Message);
                return false;
            }
        }

        private DynValue CallFunction(string function, params object[] args)
        {
            return script.Call(script.Globals.Get(function), args);
        }
    }
}