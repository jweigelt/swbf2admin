using System.Net;
using Newtonsoft.Json;
using MoonSharp.Interpreter;

namespace SWBF2Admin.Structures
{
    [MoonSharpUserData]
    public class Player
    {
        public byte Slot { get; }

        public ushort Ping { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Score { get; set; }
        public string Team { get; set; }

        public string Name { get; }
        public string KeyHash { get; }
        public bool IsBanned { get; set; }

        [JsonIgnore]
        [MoonSharpHidden]
        public IPAddress RemoteAddress { get; }

        public virtual string RemoteAddressStr { get { return RemoteAddress.ToString(); } }

        public long DatabaseId { get; set; }

        [MoonSharpHidden]
        public Player(byte slot, ushort ping, int kills, int deaths, int score, string name, string keyhash, string team, IPAddress remoteAddress)
        {
            Slot = slot;
            Ping = ping;
            Kills = kills;
            Deaths = deaths;
            Score = score;
            Name = name;
            KeyHash = keyhash;
            Team = team;
            RemoteAddress = remoteAddress;
        }

        [MoonSharpHidden]
        public Player(byte slot)
        {
            Slot = slot;
        }

     [MoonSharpHidden]
        public void CopyDbInfo(Player p)
        {
            DatabaseId = p.DatabaseId;
            IsBanned = p.IsBanned;
        }
    }
}