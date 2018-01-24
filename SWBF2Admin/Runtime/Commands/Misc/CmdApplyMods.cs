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
using SWBF2Admin.Structures;
using SWBF2Admin.Config;
using SWBF2Admin.Runtime.ApplyMods;

namespace SWBF2Admin.Runtime.Commands.Misc
{
    [ConfigFileInfo(fileName: "./cfg/cmd/applymods.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdApplyMods : ChatCommand
    {
        public string OptEnable { get; set; } = "enable";
        public string OptDisable { get; set; } = "disable";

        public string OnInvalidParams { get; set; } = "Usage: {usage}";
        public string OnInvalidAction { get; set; } = "Invalid action. Use {opt_enable} or {opt_disable}.";
        public string OnNoModFound { get; set; } = "No mod matching {input} could be found.";
        public string OnApply { get; set; } = "Applied mod {mod}";
        public string OnRevert { get; set; } = "Reverted mod {mod}";

        public CmdApplyMods() : base("applymods", "applymods", "applymods <enable/disable> <mod>") { }

        public override bool Run(Player player, string commandLine, string[] parameters)
        {
            if (parameters.Length < 2)
            {
                SendFormatted(OnInvalidParams, "{usage}", Usage);
                return false;
            }

            bool enable;
            if (!(enable = parameters[0].ToLower().Equals(OptEnable.ToLower())) &&
               !parameters[0].ToLower().Equals(OptDisable.ToLower()))
            {
                SendFormatted(OnInvalidAction, "{input}", parameters[0], "{opt_enable}", OptEnable, "{opt_disable}", OptDisable);
                return false;
            }

            LvlMod mod = null;
            foreach (LvlMod m in Core.Mods.Mods)
            {
                if (m.Name.ToLower().Equals(parameters[1].ToLower()))
                {
                    mod = m;
                    break;
                }
            }

            if (mod == null)
            {
                SendFormatted(OnNoModFound, "{input}", parameters[1]);
                return false;
            }

            if (enable)
            {
                SendFormatted(OnApply, "{mod}", mod.Name);
                Core.Mods.ApplyMod(mod);
            }
            else
            {
                SendFormatted(OnRevert, "{mod}", mod.Name);
                Core.Mods.RevertMod(mod);
            }

            return true;
        }
    }
}