using SWBF2Admin.Config;

namespace SWBF2Admin.Discord
{
    [ConfigFileInfo(fileName: "./cfg/plugins/discord.xml")]
    public class DiscordConfig
    {
        public string Token { get; set; }
        
    }
}