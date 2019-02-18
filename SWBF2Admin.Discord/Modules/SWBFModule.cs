using System;
using System.Collections.Generic;
using SWBF2Admin.Structures;
using SWBF2Admin.Utility;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SWBF2Admin.Discord.Modules
{
    public class SWBFModule : ModuleBase<SocketCommandContext>
    {
        private DiscordPlugin plugin;
        public SWBFModule(DiscordPlugin plugin)
        {
            this.plugin = plugin;
        }

        [Command("ping")]
        public async Task PingAsync()
        {
            await ReplyAsync("pong!");
        }

        [Command("players")]
        public async Task PlayersAsync()
        {
            List<Player> players = plugin.Core.Players.GetPlayers_s();
            if (players.Count > 0)
            {
                string playerList = "";
                foreach (Player p in players)
                {
                    playerList += Util.FormatString(plugin.Config.PlayerListItem,
                        "{player_name}", p.Name,
                        "{player_team}", p.Team,
                        "{player_score}", p.Score.ToString(),
                        "{player_kills}", p.Kills.ToString(),
                        "{player_deaths}", p.Deaths.ToString());
                }
                string r = Util.FormatString(plugin.Config.OnPlayerList,
                    "{players_online}", players.Count.ToString(),
                    "{players_max}", plugin.Core.Server.Settings.PlayerLimit.ToString(),
                    "{player_list}", playerList);
                await ReplyAsync(r);
            }
            else await ReplyAsync(plugin.Config.OnNoPlayersOnline);
        }

        [Command("status")]
        public async Task StatusAsync()
        {
            if (plugin.Core.Game.LatestInfo != null || true)
            {
                string s = Util.ParseVars(plugin.Config.OnStatus, plugin.Core);
                await ReplyAsync(s);
            }
            else await ReplyAsync(plugin.Config.OnStatusDown);
        }

        int GetIndex(IGuildChannel channel)
        {
            foreach (var c in plugin.Config.Channels)
            {
                if (c.Guild == channel.GuildId && c.Channel == channel.Id) return plugin.Config.Channels.IndexOf(c);
            }
            return -1;
        }

        [Command("bind")]
        public async Task BindAsync(IGuildChannel channel)
        {
            var user = Context.User as SocketGuildUser;
            if(!user.GetPermissions(channel).Has(ChannelPermission.ManageChannel))
            {
                return;
            }

            //TODO: move to template
            if (GetIndex(channel) < 0)
            {
                plugin.Config.Channels.Add(new ChannelBind(channel.GuildId, channel.Id));
                await ReplyAsync(string.Format("Bot bound to channel {0}", channel.Name));
            }
            else await ReplyAsync(string.Format("The bot is already bound to {0}", channel.Name));
        }

        [Command("unbind")]
        public async Task UnbindAsync(IGuildChannel channel)
        {
            var user = Context.User as SocketGuildUser;
            if (!user.GetPermissions(channel).Has(ChannelPermission.ManageChannel))
            {
                return;
            }

            //TODO: move to template
            int idx;
            if ((idx = GetIndex(channel)) > 0)
            {
                plugin.Config.Channels.RemoveAt(idx);
                await ReplyAsync(string.Format("Removed channel bind to {0}", channel.Name));
            }
            else await ReplyAsync(string.Format("The bot is not bound to {0}", channel.Name));
        }

    }
}