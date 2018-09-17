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
    class DiscordPlugin : SWBF2AdminPlugin
    {
        private DiscordSocketClient client;
        public DiscordPlugin(AdminCore core) : base(core, "DiscordPlugin", "1.0")
        {
            Logger.Log(LogLevel.Info, "[DISCORD] Loaded {0} v{1}", Name, Version);
            Initialize();
        }
        private DiscordConfig config;

        private async void Initialize()
        {
            config = core.Files.ReadConfig<DiscordConfig>();

            var services = ConfigureServices();
            client = services.GetRequiredService<DiscordSocketClient>();
            client.Log += LogAsync;

            services.GetRequiredService<CommandService>().Log += LogAsync;
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            await Task.Delay(-1);

            core.Rcon.ChatInput += new EventHandler(Rcon_Chat);
        }

        private void Rcon_Chat(object sender, EventArgs ec)
        {
            RconChatEventArgs e = (RconChatEventArgs)ec;

            client.GetGuild(12).GetTextChannel(12).SendMessageAsync(string.Format("{0}: {1}", e.Message.PlayerName, e.Message.Message));
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
                .BuildServiceProvider();
        }
    }
}