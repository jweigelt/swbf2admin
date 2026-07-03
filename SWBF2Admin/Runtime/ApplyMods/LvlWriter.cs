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
using System.Xml;

namespace SWBF2Admin.Runtime.ApplyMods
{
    public class LvlWriter : ComponentBase
    {
        private string serverDir;
        private LvlWriterConfig config;
        //Remembers which file the config was loaded from so SaveConfig() writes back to the same one.
        private string configFileName = "";
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
                //Empty filename lets FileHandler resolve the default path from LvlWriterConfig's ConfigFileInfo.
                configFileName = "";
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
                mod.Apply(Core.Files, serverDir);
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
                mod.Revert(Core.Files, serverDir);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to apply mod \"{0}\" {1}", mod.Name, e.Message);
            }
        }

        //Rewrites only the Enabled attribute in place so hand-authored XML comments and formatting
        //survive. Falls back to a full serialize (losing comments) if the file is missing or editing fails.
        public void SaveConfig()
        {
            string fileName = string.IsNullOrEmpty(configFileName)
                ? GetConfigFileName()
                : configFileName;

            if (!File.Exists(fileName))
            {
                Core.Files.WriteConfig(config, configFileName);
                return;
            }

            try
            {
                UpdateConfigInPlace(fileName);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to update \"{0}\" in place, rewriting it: {1}", fileName, e.Message);
                Core.Files.WriteConfig(config, configFileName);
            }
        }

        private static string GetConfigFileName()
        {
            ConfigFileInfo[] info = (ConfigFileInfo[])typeof(LvlWriterConfig)
                .GetCustomAttributes(typeof(ConfigFileInfo), false);
            if (info.Length == 0)
                throw new Exception("No [ConfigFileInfo] attribute on LvlWriterConfig.");
            return info[0].FileName;
        }

        //Rewrites only the Enabled attribute on each <LvlMod>. File mods only use Enabled;
        //ApplyOnStart/RevertOnStart are a process-mod concept and are left untouched here.
        private void UpdateConfigInPlace(string fileName)
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(fileName);

            foreach (LvlMod mod in config.Mods)
            {
                XmlElement node = FindModNode(doc, mod.Name);
                if (node == null) continue;

                node.SetAttribute("Enabled", XmlConvert.ToString(mod.Enabled));
            }

            doc.Save(fileName);
            Logger.Log(LogLevel.Verbose, "Updated mods config \"{0}\" in place.", fileName);
        }

        private static XmlElement FindModNode(XmlDocument doc, string name)
        {
            foreach (XmlNode node in doc.GetElementsByTagName("LvlMod"))
            {
                if (node is XmlElement element &&
                    string.Equals(element.GetAttribute("Name"), name, StringComparison.Ordinal))
                {
                    return element;
                }
            }
            return null;
        }
    }
}
