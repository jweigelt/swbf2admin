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
using SWBF2Admin.Config;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace SWBF2Admin.Runtime.ApplyMods
{
    public class LvlWriter : ComponentBase
    {
        private string serverDir;
        private LvlWriterConfig config;
        private string configFileName = "";
        private readonly object modLock = new object();
        public LvlWriter(AdminCore core) : base(core) { }
        public virtual List<LvlMod> Mods { get { return config?.Mods ?? new List<LvlMod>(); } }

        public override void Configure(CoreConfiguration config)
        {
            if (config.ServerType == GameserverType.Aspyr)
            {
                //Unpack the Aspyr mods
                configFileName = "./cfg/mods.aspyr.xml";
                this.config = Core.Files.ReadConfig<LvlWriterConfig>(configFileName, "SWBF2Admin.Resources.cfg.mods.aspyr.xml");
            } else
            {
                configFileName = Core.Files.GetConfigFileName<LvlWriterConfig>();
                this.config = Core.Files.ReadConfig<LvlWriterConfig>();
            }
            serverDir = Core.Files.ParseFileName(config.ServerPath);

            //Handle legacy mods.xml
            foreach (LvlMod mod in this.config.Mods)
            {
                foreach (HexEdit he in mod.HexEdits)
                {
                    if (string.IsNullOrEmpty(he.LevelDir))
                    {
                        he.LevelDir = this.config.LvlDir;
                    }
                }
            }
        }

        public override void OnInit()
        {
            base.OnInit();
        }

        public void RevertAll()
        {
            lock (modLock)
            {
                foreach (LvlMod mod in config.Mods)
                {
                    if (mod.RevertOnStart) ApplyMod(mod);
                    else if (mod.ApplyOnStart) RevertMod(mod);
                }
            }
        }

        public void ApplyMod(LvlMod mod)
        {
            lock (modLock)
            {
                try
                {
                    mod.Apply(Core.Files, serverDir);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, "Failed to apply mod \"{0}\" {1}", mod.Name, e.Message);
                }
            }
        }

        public void RevertMod(LvlMod mod)
        {
            lock (modLock)
            {
                try
                {
                    mod.Revert(Core.Files, serverDir);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, "Failed to revert mod \"{0}\" {1}", mod.Name, e.Message);
                }
            }
        }

        public void SetModEnabled(LvlMod mod, bool enabled)
        {
            lock (modLock)
            {
                mod.Enabled = enabled;
                SaveConfig();
                if (enabled) ApplyMod(mod);
                else RevertMod(mod);
            }
        }

        //Preserve XML comments unless the targeted update fails
        private void SaveConfig()
        {
            if (!File.Exists(configFileName))
            {
                Core.Files.WriteConfig(config, configFileName);
                return;
            }

            try
            {
                Dictionary<string, string> values = new Dictionary<string, string>();
                foreach (LvlMod mod in config.Mods)
                    values[mod.Name] = mod.Enabled.ToString().ToLowerInvariant();

                Core.Files.UpdateConfigAttributes(configFileName, "LvlMod", "Name", "Enabled", values);
                Logger.Log(LogLevel.Verbose, "Updated mods config \"{0}\" in place.", configFileName);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to update \"{0}\" in place, rewriting it: {1}", configFileName, e.Message);
                Core.Files.WriteConfig(config, configFileName);
            }
        }
    }
}
