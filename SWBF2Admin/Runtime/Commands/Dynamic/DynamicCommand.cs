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
using System;
using System.IO;
using System.Xml.Serialization;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;
using SWBF2Admin.Runtime.Permissions;

using MoonSharp.Interpreter;

namespace SWBF2Admin.Runtime.Commands.Dynamic
{
    public class DynamicCommand : ChatCommand
    {
        [XmlIgnore]
        public override Permission Permission { get { return Permission.FromName(PermissionName); } }

        public string PermissionName { get; set; }

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

            if (PermissionName == null)
            {
                Logger.Log(LogLevel.Warning, "{0} has no valid PermissionName definition: disabling it.", name);
                Enabled = false;
            }
        }

        public override bool Run(Player player, string commandLine, string[] parameters)
        {
            try
            {
                return CallFunction(LuaApi.FUNC_RUN, player, commandLine, parameters).Boolean;
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