using SWBF2Admin.Plugins;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Discord
{
    class DiscordPlugin : SWBF2AdminPlugin
    {
        public DiscordPlugin(AdminCore core) : base(core, "DiscordPlugin", "1.0")
        {
            Logger.Log(LogLevel.Info, "Loaded {0} v{1}", Name, Version);
        }
    }
}