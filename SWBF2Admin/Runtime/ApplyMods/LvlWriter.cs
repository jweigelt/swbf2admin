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
using System.Collections.Generic;
using SWBF2Admin.Config;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.ApplyMods
{
    public class LvlWriter : ComponentBase
    {
        private string levelDir;
        private LvlWriterConfig config;
        public LvlWriter(AdminCore core) : base(core) { }
        public virtual List<LvlMod> Mods { get { return config.Mods; } }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<LvlWriterConfig>();
            levelDir = Core.Files.ParseFileName(config.ServerPath) + this.config.LvlDir;
        }

        public override void OnInit()
        {
            base.OnInit();
        }

        public void RevertAll()
        {
            foreach (LvlMod mod in config.Mods)
            {
                if (mod.RevertOnStart) ApplyMod(mod);
                else if (mod.ApplyOnStart) RevertMod(mod);
            }
        }

        public void ApplyMod(LvlMod mod)
        {
            try
            {
                mod.Apply(Core.Files, levelDir);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to revert mod \"{0}\" {1}", mod.Name, e.Message);
            }
        }

        public void RevertMod(LvlMod mod)
        {
            try
            {
                mod.Revert(Core.Files, levelDir);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to apply mod \"{0}\" {1}", mod.Name, e.Message);
            }
        }
    }
}
