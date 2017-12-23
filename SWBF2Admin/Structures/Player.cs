using System.Net;
using Newtonsoft.Json;

namespace SWBF2Admin.Structures
{
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

        public void CopyDbInfo(Player p)
        {
            DatabaseId = p.DatabaseId;
            IsBanned = p.IsBanned;
        }
    }
}