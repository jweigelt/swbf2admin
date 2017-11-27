using System;
using Newtonsoft.Json;

namespace SWBF2Admin.Structures
{
    enum BanType
    {
        ShowAll = -1,   //only used for webadmin
        Keyhash = 0,
        IPAddress = 1
    }

    class PlayerBan
    {
        public long DatabaseId { get; }

        [JsonIgnore]
        public DateTime Date { get; }
        public virtual string DateStr { get { return Date.ToString(); } }

        public long Duration { get; }
        public virtual bool Expired { get { return (((DateTime.Now - Date).TotalSeconds > Duration) && Duration > 0); } }

        [JsonIgnore]
        public BanType Type { get; }
        public virtual int TypeId { get { return (int)Type; } }

        public string PlayerName { get; }
        public string PlayerKeyhash { get; }
        public string PlayerIPAddress { get; }

        public string AdminName { get; }
        public string Reason { get; }

        public PlayerBan(long databaseId, string playerName, string playerKeyhash, string playerIPAddress, string adminName, string reason, DateTime date, long duration, BanType type)
        {
            DatabaseId = databaseId;
            PlayerName = playerName;
            PlayerKeyhash = playerKeyhash;
            PlayerIPAddress = playerIPAddress;
            AdminName = adminName;
            Date = date;
            Duration = duration;
            Type = type;
            Reason = reason;
        }
    }
}