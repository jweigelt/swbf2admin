using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SWBF2Admin.Discord.Modules;

namespace SWBF2Admin.Discord.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService commands;
        private readonly DiscordSocketClient discord;
        private readonly IServiceProvider services;
        private readonly DiscordPlugin plugin;

        public CommandHandlingService(IServiceProvider services)
        {
            this.services = services;
            commands = services.GetRequiredService<CommandService>();
            discord = services.GetRequiredService<DiscordSocketClient>();
            plugin = services.GetRequiredService<DiscordPlugin>();
            discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            await commands.AddModuleAsync<SWBFModule>();
        }

        private bool IsChannelBound(IChannel channel)
        {
            foreach (ChannelBind b in plugin.Config.Channels)
            {
                //TODO: guildID not required?
                if (channel.Id == b.Channel) return true;
            }
            return false;
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var argPos = 0;
            if (!message.HasStringPrefix(plugin.Config.BotPrefix + " ", ref argPos)) return;
            //if (!IsChannelBound(message.Channel)) return;

            var context = new SocketCommandContext(discord, message);
            var result = await commands.ExecuteAsync(context, argPos, services);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand) // it's bad practice to send 'unknown command' errors
                await context.Channel.SendMessageAsync(result.ToString());
        }
    }
}