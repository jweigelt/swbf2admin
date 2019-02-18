using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using SWBF2Admin.Plugins;
using SWBF2Admin.Utility;
using SWBF2Admin.Runtime.Rcon;

using SWBF2Admin.Discord.Services;

namespace SWBF2Admin.Discord
{
    public class DiscordPlugin : SWBF2AdminPlugin
    {
        public AdminCore Core { get; }
        public DiscordConfig Config { get; set; }

        private DiscordSocketClient client;
        public DiscordPlugin(AdminCore core) : base(core, "DiscordPlugin", "1.0")
        {
            Core = core;
            Logger.Log(LogLevel.Info, "[DISCORD] Loaded {0} v{1}", Name, Version);
            Initialize();
        }

        private async void Initialize()
        {
            Config = core.Files.ReadConfig<DiscordConfig>();

            var services = ConfigureServices();
            client = services.GetRequiredService<DiscordSocketClient>();
            client.Log += LogAsync;

            services.GetRequiredService<CommandService>().Log += LogAsync;
            await client.LoginAsync(TokenType.Bot, Config.Token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            core.Rcon.ChatInput += new EventHandler(Rcon_Chat);
            await Task.Delay(-1);
        }

        private void Rcon_Chat(object sender, EventArgs ec)
        {
            RconChatEventArgs e = (RconChatEventArgs)ec;
            
            foreach(var c in Config.Channels)
            {
                string s = Util.FormatString(Config.OnChat, "{bot_prefix}", Config.BotPrefix, "{player_name}", e.Message.PlayerName, "{message}", e.Message.Message);
                client.GetGuild(c.Guild).GetTextChannel(c.Channel).SendMessageAsync(s);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            //TODO: proxy severity
            Logger.Log(LogLevel.Info, "[DISCORD] {0}", log.Message);
            return Task.CompletedTask;
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton(serviceProvider => {
                    return this;
                })
                .BuildServiceProvider();
        }
    }
}