using System.Threading.Tasks;
using Discord;
using Discord.Commands;
namespace SWBF2Admin.Discord.Modules
{
    public class SWBFModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task PingAsync() { 
            await ReplyAsync("pong!");
        }

        [Command("bind")]
        public async Task BindAsync(IGuildChannel channel)
        {
            await ReplyAsync(string.Format("Bot bound to channel {0}", channel.Name));
        }
    }
}