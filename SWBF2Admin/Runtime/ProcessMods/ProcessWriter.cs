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
        private string configFileName = "";

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

            //Start each mod in the state saved for the next server launch
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
                        } else if (mod.RevertOnStart)
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
            reader.Close();

            foreach (ProcessMod mod in Mods)
                foreach (CodeCave cave in mod.CodeCaves)
                    cave.ResetAllocation();

            DisableUpdates();
        }

        public override void OnDeInit()
        {
            ProcessOpened = false;
            reader.Close();
        }
        public void ApplyMod(ProcessMod mod)
        {
            mod.Apply(reader);
        }
        public void RevertMod(ProcessMod mod)
        {
            mod.Revert(reader);
        }

        //Patch in place to keep XML comments; rewrite only on failure
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

        //Only persists ApplyOnStart (Enabled is runtime-only)
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
            //Do not reopen the reader when startup events overlap
            if (ProcessOpened && reader.IsProcessOpen && Core.Server.ServerProcess != null)
            {
                try
                {
                    if (!Core.Server.ServerProcess.HasExited &&
                        reader.ProcessId == Core.Server.ServerProcess.Id)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    //The server changed or exited; reopen the reader
                }
            }

            ProcessOpened = false;
            reader.Close();
            string lastError = null;

            Logger.Log(LogLevel.Verbose, "Trying to open process module \"{0}\"...", moduleName);

            for (int i = 0; i < maxAttempts; i++)
            {
                System.Diagnostics.Process process = Core.Server.ServerProcess;
                if (process == null)
                    return false;

                try
                {
                    if (process.HasExited)
                        return false;

                    if (reader.Open(process, moduleName))
                    {
                        //Do not keep a handle opened for an earlier server process
                        System.Diagnostics.Process currentProcess = Core.Server.ServerProcess;
                        if (currentProcess == null || currentProcess.Id != reader.ProcessId)
                        {
                            lastError = "The server process changed while the reader was attaching.";
                            reader.Close();
                        }
                        else
                        {
                            Logger.Log(LogLevel.Info, "Opened process reader to module \"{0}\", target64={1}.", moduleName, reader.IsTarget64Bit.ToString());
                            ProcessOpened = true;
                            return true;
                        }
                    }
                    else
                    {
                        lastError = reader.LastOpenError;
                    }
                }
                catch (Exception ex)
                {
                    //Errors while the process starts are temporary; keep trying until the retry limit
                    lastError = ex.Message;
                    reader.Close();
                }

                if (i + 1 < maxAttempts)
                    System.Threading.Thread.Sleep(sleepMs);
            }

            Logger.Log(LogLevel.Warning,
                "Failed to attach process reader to module \"{0}\" after {1} attempts. Last error: {2}",
                moduleName, maxAttempts.ToString(), lastError ?? "unknown");
            return false;
        }
    }
}
