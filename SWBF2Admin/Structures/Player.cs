using System.Net;
using Newtonsoft.Json;

namespace SWBF2Admin.Structures
{
    public class Player
    {
        public byte Slot { get; }

        public ushort Ping { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public int Score { get; }

        public string Name { get; set; }
        public string KeyHash { get; set; }
        public string Team { get; set; }

        [JsonIgnore]
        public IPAddress RemoteAddress { get; }
        public virtual string RemoteAddressStr { get { return RemoteAddress.ToString(); } }

        public long DatabaseId { get; set; }

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

    }
}