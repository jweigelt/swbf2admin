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
