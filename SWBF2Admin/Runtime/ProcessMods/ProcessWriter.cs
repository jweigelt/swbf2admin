using SWBF2Admin.Config;
using SWBF2Admin.Runtime.ProcessMods;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SWBF2Admin.Runtime.Readers
{
    public class ProcessWriter : ComponentBase
    {
        public virtual List<ProcessMod> Mods { get { return _config?.Mods ?? new List<ProcessMod>(); } }

        public ProcessMemoryReader reader = new ProcessMemoryReader();
        private ProcessWriterConfig _config;
        public ProcessWriter(AdminCore core) : base(core) { }
        public bool IsWarmup = true;
        private string moduleName = "BattlefrontII.exe";
        private string configFileName = "";
        private readonly object modLock = new object();

        public override void Configure(CoreConfiguration config)
        {
            if (config.ServerType == GameserverType.Aspyr)
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
                configFileName = Core.Files.GetConfigFileName<ProcessWriterConfig>();
                _config = Core.Files.ReadConfig<ProcessWriterConfig>();
            }
        }

        public override void OnInit()
        {
            lock (modLock)
            {
                if (Core.Server.ServerProcess != null)
                {
                    Logger.Log(LogLevel.Verbose, "Found running process. Trying to open reader");
                    TryOpenReader();
                }
            }
        }
        public override void OnServerStart(EventArgs e)
        {
            lock (modLock)
            {
                if (TryOpenReader())
                {
                    foreach (ProcessMod mod in Mods)
                    {
                        try
                        {
                            if (mod.ApplyOnStart)
                            {
                                ApplyMod(mod);
                            } else if (mod.RevertOnStart)
                            {
                                RevertMod(mod);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogLevel.Warning, "Failed to apply process mod \"{0}\" {1}", mod.Name, ex.Message);
                        }
                    }
                }
            }
            EnableUpdates();
        }

        public override void OnServerStop()
        {
            lock (modLock)
            {
                reader.Close();

                foreach (ProcessMod mod in Mods)
                    foreach (CodeCave cave in mod.CodeCaves)
                        cave.ResetAllocation();
            }

            DisableUpdates();
        }

        public override void OnDeInit()
        {
            lock (modLock)
            {
                reader.Close();
            }
        }
        public void ApplyMod(ProcessMod mod)
        {
            lock (modLock)
            {
                mod.Apply(reader);
            }
        }
        public void RevertMod(ProcessMod mod)
        {
            lock (modLock)
            {
                mod.Revert(reader);
            }
        }

        public bool SetModEnabled(ProcessMod mod, bool enabled)
        {
            lock (modLock)
            {
                mod.Enabled = enabled;
                SaveConfig();

                if (Core.Server.Status != Gameserver.ServerStatus.Online || !reader.IsProcessOpen)
                    return false;

                if (enabled) ApplyMod(mod);
                else RevertMod(mod);
                return true;
            }
        }

        //Preserve XML comments unless the targeted update fails
        private void SaveConfig()
        {
            if (!File.Exists(configFileName))
            {
                Core.Files.WriteConfig(_config, configFileName);
                return;
            }

            try
            {
                Dictionary<string, string> values = new Dictionary<string, string>();
                foreach (ProcessMod mod in _config.Mods)
                    values[mod.Name] = mod.ApplyOnStart.ToString().ToLowerInvariant();

                Core.Files.UpdateConfigAttributes(configFileName, "ProcessMod", "Name", "ApplyOnStart", values);
                Logger.Log(LogLevel.Verbose, "Updated process mods config \"{0}\" in place.", configFileName);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Failed to update \"{0}\" in place, rewriting it: {1}", configFileName, e.Message);
                Core.Files.WriteConfig(_config, configFileName);
            }
        }

        private bool TryOpenReader(int maxAttempts = 100, int sleepMs = 100)
        {
            //Do not reopen the reader when startup events overlap
            Process process = Core.Server.ServerProcess;
            if (reader.IsProcessOpen && process != null)
            {
                try
                {
                    if (!process.HasExited &&
                        reader.ProcessId == process.Id &&
                        ReferenceEquals(process, Core.Server.ServerProcess))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    //The server changed or exited; reopen the reader
                }
            }

            reader.Close();
            string lastError = null;

            Logger.Log(LogLevel.Verbose, "Trying to open process module \"{0}\"...", moduleName);

            for (int i = 0; i < maxAttempts; i++)
            {
                process = Core.Server.ServerProcess;
                if (process == null)
                    return false;

                try
                {
                    if (process.HasExited)
                        return false;

                    if (reader.Open(process, moduleName))
                    {
                        //Do not keep a handle opened for an earlier server process
                        Process currentProcess = Core.Server.ServerProcess;
                        if (currentProcess == null || currentProcess.Id != reader.ProcessId)
                        {
                            lastError = "The server process changed while the reader was attaching.";
                            reader.Close();
                        }
                        else
                        {
                            Logger.Log(LogLevel.Info, "Opened process reader to module \"{0}\", target64={1}.", moduleName, reader.IsTarget64Bit.ToString());
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
                    Thread.Sleep(sleepMs);
            }

            Logger.Log(LogLevel.Warning,
                "Failed to attach process reader to module \"{0}\" after {1} attempts. Last error: {2}",
                moduleName, maxAttempts.ToString(), lastError ?? "unknown");
            return false;
        }
    }
}
