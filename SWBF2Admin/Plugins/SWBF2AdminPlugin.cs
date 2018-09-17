using System;

namespace SWBF2Admin.Plugins
{
    public abstract class SWBF2AdminPlugin
    {
        public string Name { get;}
        public string Version { get; }
        protected AdminCore core;

        public SWBF2AdminPlugin(AdminCore core, string name, string version)
        {
            this.core = core;
            Name = name;
            Version = version;
        }
    }
}