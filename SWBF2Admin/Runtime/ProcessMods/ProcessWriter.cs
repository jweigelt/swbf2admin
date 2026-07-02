using SWBF2Admin.Config;
using SWBF2Admin.Runtime.ProcessMods;
using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;

namespace SWBF2Admin.Runtime.Readers
{
    public class ProcessWriter : ComponentBase
    {
        public virtual List<ProcessMod> Mods { get { return config.Mods; } }

        public bool ProcessOpened;
        public ProcessMemoryReader reader = new ProcessMemoryReader();
        private ProcessWriterConfig config;
        public ProcessWriter(AdminCore core) : base(core) { }
        public bool IsWarmup = true;
        private string moduleName = "BattlefrontII.exe";

        public override void Configure(CoreConfiguration config)
        {
            // Implement the configuration logic for your memory reader
            this.config = Core.Files.ReadConfig<ProcessWriterConfig>();
            if (config.ServerType == GameserverType.Aspyr)
            {
                moduleName = "Battlefront2.dll";
            }
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
                            mod.Apply(reader);

                        }else if (mod.RevertOnStart)
                        {
                            mod.Revert(reader);
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
            mod.Apply(reader);
        }
        public void RevertMod(ProcessMod mod)
        {
            mod.Revert(reader);
        }

        private bool TryOpenReader(int maxAttempts = 100, int sleepMs = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                if (Core.Server.ServerProcess == null || Core.Server.ServerProcess.HasExited)
                    return false;

                if (reader.Open(Core.Server.ServerProcess, moduleName))
                {
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
