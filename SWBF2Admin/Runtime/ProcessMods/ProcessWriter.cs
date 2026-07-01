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

        public override void Configure(CoreConfiguration config)
        {
            // Implement the configuration logic for your memory reader
            this.config = Core.Files.ReadConfig<ProcessWriterConfig>();
        }

        public override void OnInit()
        {
            if (Core.Server.ServerProcess != null)
            {
                Logger.Log(LogLevel.Verbose, "Found running process. Trying to open reader");
                if (reader.Open(Core.Server.ServerProcess))
                {
                    ProcessOpened = true;
                }
            }
        }
        public override void OnServerStart(EventArgs e)
        {
            if (reader.Open(Core.Server.ServerProcess))
            {
                ProcessOpened = true;
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
    }
}
