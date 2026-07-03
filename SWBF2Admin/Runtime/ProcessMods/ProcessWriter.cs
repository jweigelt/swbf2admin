using SWBF2Admin.Config;
using SWBF2Admin.Runtime.ProcessMods;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SWBF2Admin.Runtime.Readers
{
    public class ProcessWriter : ComponentBase
    {
        public virtual List<ProcessMod> Mods { get { return _config?.Mods ?? new List<ProcessMod>(); } }

        public bool ProcessOpened;
        public ProcessMemoryReader reader = new ProcessMemoryReader();
        private ProcessWriterConfig _config;
        public ProcessWriter(AdminCore core) : base(core) { }
        public bool IsWarmup = true;
        private string moduleName = "BattlefrontII.exe";
        private bool isAspyr = false;
        //Remembers which file the config was loaded from so SaveConfig() writes back to the same one.
        private string configFileName = "";

        //Aspyr-only process mod used to override the game's spawn delay float.
        private const string SPAWN_DELAY_MOD = "spawn_delay";
        //Aspyr-only process mod used to override the game's platform lobby string.
        private const string PLATFORM_MOD = "platform_lobby";

        public override void Configure(CoreConfiguration config)
        {
            isAspyr = config.ServerType == GameserverType.Aspyr;
            if (isAspyr)
            {
                moduleName = "Battlefront2.dll";
                reader.SetTargetPointerSize(8);
                configFileName = "./cfg/process_mods.aspyr.xml";
                _config = Core.Files.ReadConfig<ProcessWriterConfig>(configFileName, "SWBF2Admin.Resources.cfg.process_mods.aspyr.xml");
            }
            else
            {
                moduleName = "BattlefrontII.exe";
                reader.SetTargetPointerSize(4);
                configFileName = "";
                _config = Core.Files.ReadConfig<ProcessWriterConfig>();
            }

            foreach (ProcessMod mod in _config.Mods)
                mod.Enabled = mod.ApplyOnStart;
        }

        public override void OnInit()
        {
            if (Core.Server.ServerProcess != null)
            {
                Logger.Log(LogLevel.Verbose, "Found running process. Trying to open reader");
                TryOpenReader();
            }
        }
        public override void OnServerStart(EventArgs e)
        {
            if (TryOpenReader())
            {
                foreach (ProcessMod mod in Mods)
                {
                    try
                    {
                        if (mod.ApplyOnStart)
                        {
                            mod.Enabled = true;
                            ApplyMod(mod);

                        }else if (mod.RevertOnStart)
                        {
                            RevertMod(mod);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Warning, "Failed to apply process mod \"{0}\" {1}", mod.Name, ex.Message);
                    };
                }
            }
            EnableUpdates();
        }

        public override void OnServerStop()
        {
            ProcessOpened = false;
            DisableUpdates();
        }
        public void ApplyMod(ProcessMod mod)
        {
            if (isAspyr && mod.Name == SPAWN_DELAY_MOD)
            {
                ApplySpawnDelay();
                return;
            }
            if (isAspyr && mod.Name == PLATFORM_MOD)
            {
                ApplyPlatform();
                return;
            }
            mod.Apply(reader);
        }
        public void RevertMod(ProcessMod mod)
        {
            mod.Revert(reader);
        }

        //Writes the WebAdmin "Spawn Delay" field (Settings.AutoAnnouncePeriod) into the Aspyr process via the spawn_delay mod.
        //GOG/Steam handle this through the SPAWN_TIMER env variable read by RconServer instead.
        public void ApplySpawnDelay()
        {
            if (!isAspyr || !ProcessOpened) return;

            ProcessMod mod = Mods.Find(m => m.Name == SPAWN_DELAY_MOD);
            if (mod == null || mod.ProcessEdits.Count == 0) return;

            //Match the mod's big-endian IEEE-754 float layout (e.g. 15 -> 41700000).
            byte[] patchedBytes = BitConverter.GetBytes((float)Core.Server.Settings.AutoAnnouncePeriod);
            if (BitConverter.IsLittleEndian) Array.Reverse(patchedBytes);
            mod.ProcessEdits[0].PatchedBytes = patchedBytes;

            try
            {
                mod.Apply(reader);
                Logger.Log(LogLevel.Info, "Set spawn delay to {0}s", Core.Server.Settings.AutoAnnouncePeriod.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Failed to apply spawn delay {0}", ex.Message);
            }
        }

        //Writes the WebAdmin "Platform Lobby" field (Settings.Platform) into the Aspyr process via the platform_lobby mod.
        public void ApplyPlatform()
        {
            if (!isAspyr || !ProcessOpened) return;

            ProcessMod mod = Mods.Find(m => m.Name == PLATFORM_MOD);
            if (mod == null || mod.ProcessEdits.Count == 0) return;

            //Every valid platform is a two-byte ASCII code (pc/ps/xb/ns).
            string platform = Core.Server.Settings.Platform;
            if (string.IsNullOrEmpty(platform) || platform.Length != 2) return;
            mod.ProcessEdits[0].PatchedBytes = System.Text.Encoding.ASCII.GetBytes(platform);

            try
            {
                mod.Apply(reader);
                Logger.Log(LogLevel.Info, "Set server platform to \"{0}\"", platform);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Failed to apply server platform {0}", ex.Message);
            }
        }

        //Rewrites only the toggled attributes in place so hand-authored XML comments and formatting
        //survive. Falls back to a full serialize (losing comments) if the file is missing or editing fails.
        public void SaveConfig()
        {
            string fileName = string.IsNullOrEmpty(configFileName)
                ? GetConfigFileName()
                : configFileName;

            if (!File.Exists(fileName))
            {
                Core.Files.WriteConfig(_config, configFileName);
                return;
            }

            try
            {
                UpdateConfigInPlace(fileName);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to update \"{0}\" in place, rewriting it: {1}", fileName, e.Message);
                Core.Files.WriteConfig(_config, configFileName);
            }
        }

        private string GetConfigFileName()
        {
            ConfigFileInfo[] info = (ConfigFileInfo[])typeof(ProcessWriterConfig)
                .GetCustomAttributes(typeof(ConfigFileInfo), false);
            if (info.Length == 0)
                throw new Exception("No [ConfigFileInfo] attribute on ProcessWriterConfig.");
            return info[0].FileName;
        }

        //Rewrites only the ApplyOnStart attribute on each <ProcessMod>. Enabled is runtime-only and not
        //persisted (see ProcessMod.Enabled); toggling a mod updates ApplyOnStart so it starts next launch.
        private void UpdateConfigInPlace(string fileName)
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(fileName);

            foreach (ProcessMod mod in _config.Mods)
            {
                XmlElement node = FindModNode(doc, mod.Name);
                if (node == null) continue;

                node.SetAttribute("ApplyOnStart", XmlConvert.ToString(mod.ApplyOnStart));
            }

            doc.Save(fileName);
            Logger.Log(LogLevel.Verbose, "Updated process mods config \"{0}\" in place.", fileName);
        }

        private static XmlElement FindModNode(XmlDocument doc, string name)
        {
            foreach (XmlNode node in doc.GetElementsByTagName("ProcessMod"))
            {
                if (node is XmlElement element &&
                    string.Equals(element.GetAttribute("Name"), name, StringComparison.Ordinal))
                {
                    return element;
                }
            }
            return null;
        }

        private bool TryOpenReader(int maxAttempts = 100, int sleepMs = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                if (Core.Server.ServerProcess == null || Core.Server.ServerProcess.HasExited)
                    return false;

                if (reader.Open(Core.Server.ServerProcess, moduleName))
                {
                    Logger.Log(LogLevel.Info, "Opened process reader to module \"{0}\", target64={1}.", moduleName, reader.IsTarget64Bit.ToString());
                    ProcessOpened = true;
                    return true;
                }

                System.Threading.Thread.Sleep(sleepMs);

            }

            Logger.Log(LogLevel.Warning, "Failed to attach process reader to module \"{0}\".", moduleName);
            return false;
        }
    }
}
