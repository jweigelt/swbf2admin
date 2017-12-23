using System;
using Newtonsoft.Json;

namespace SWBF2Admin.Structures
{
    public enum BanType
    {
        ShowAll = -1,   //only used for webadmin
        Keyhash = 0,
        IPAddress = 1
    }

    public class PlayerBan
    {
        public const int DURATION_PERMANENT = -1;
        public long DatabaseId { get; }

        [JsonIgnore]
        public DateTime Date { get; }
        public virtual string DateStr { get { return Date.ToString(); } }

        public long Duration { get; }
        public virtual bool Expired { get { return ((Date.AddSeconds(Duration) < DateTime.Now) && Duration > 0); } }

        [JsonIgnore]
        public BanType Type { get; }
        public virtual int TypeId { get { return (int)Type; } }

        public string PlayerName { get; }
        public string PlayerKeyhash { get; }
        public string PlayerIPAddress { get; }

        public string AdminName { get; }
        public string Reason { get; }

        public long PlayerDatabaseId { get; }
        public long AdminDatabaseId { get; }

        public PlayerBan(long databaseId, string playerName, string playerKeyhash, string playerIPAddress, string adminName, string reason, DateTime date, long duration, BanType type, long playerDatabaseId, long adminDatabaseId)
        {
            DatabaseId = databaseId;

            PlayerName = playerName;
            PlayerKeyhash = playerKeyhash;
            PlayerIPAddress = playerIPAddress;
            PlayerDatabaseId = playerDatabaseId;

            AdminName = adminName;
            AdminDatabaseId = adminDatabaseId;
            Date = date;
            Duration = duration;
            Type = type;
            Reason = reason;
        }

        public PlayerBan(string playerName, string playerKeyhash, string playerIPAddress, string adminName, string reason, TimeSpan duration, BanType type, long playerDatabaseId, long adminDatabaseId)
        {
            PlayerName = playerName;
            PlayerKeyhash = playerKeyhash;
            PlayerIPAddress = playerIPAddress;
            PlayerDatabaseId = playerDatabaseId;

            AdminName = adminName;
            AdminDatabaseId = adminDatabaseId;
            Duration = (long)duration.TotalSeconds;
            Type = type;
            Reason = reason;
        }
        public PlayerBan(string playerName, string playerKeyhash, string playerIPAddress, string adminName, string reason, BanType type, long playerDatabaseId, long adminDatabaseId)
        {
            PlayerName = playerName;
            PlayerKeyhash = playerKeyhash;
            PlayerIPAddress = playerIPAddress;
            PlayerDatabaseId = playerDatabaseId;

            AdminName = adminName;
            AdminDatabaseId = adminDatabaseId;
            Duration = DURATION_PERMANENT;
            Type = type;
            Reason = reason;
        }
    }
}