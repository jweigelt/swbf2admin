using System.Collections.Generic;
using System.Xml.Serialization;
using SWBF2Admin.Config;

namespace SWBF2Admin.Discord
{
    public class ChannelBind
    {
        [XmlAttribute]
        public ulong Guild { get; set; }
        [XmlAttribute]
        public ulong Channel { get; set; }

        public ChannelBind() { }
        public ChannelBind(ulong guild, ulong channel)
        {
            Guild = guild;
            Channel = channel;
        }
    }

    [ConfigFileInfo(fileName: "./cfg/plugins/discord.xml")]
    public class DiscordConfig
    {
        //bot config
        public string Token { get; set; }
        public string BotPrefix { get; set; }
        public List<ChannelBind> Channels { get; set; }

        //template section
        //NOTE: using only one cfg file to keep plugin configuration in one place
        public string OnNoPlayersOnline { get; set; } = "No players online";
        public string OnPlayerList { get; set; } = "{players_online} / {players_max} players online: \n{player_list}";
        public string PlayerListItem { get; set; } = "\"{player_name}\" - {player_team} {player_score}p, {player_kills}k, {player_deaths}d";
        public string OnStatus { get; set; } = "{s:name} \nPlayers:{s:players} / {s:maxplayers}\nMap:{s:map} ,next: {s:nextmap}\nTickets: {s:t1tickets}/{s:t2tickets}";
        public string OnStatusDown { get; set; } = "The server is stopped.";
        public string OnChat { get; set; } = "[{bot_prefix}] #{player_name}: {message}";

        public DiscordConfig()
        {
            Channels = new List<ChannelBind>();
        }
    }
}