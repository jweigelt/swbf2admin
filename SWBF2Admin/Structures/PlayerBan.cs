/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
using System;
using Newtonsoft.Json;
using MoonSharp.Interpreter;

namespace SWBF2Admin.Structures
{
    public enum BanType
    {
        ShowAll = -1,   //only used for webadmin
        Keyhash = 0,
        IPAddress = 1
    }

    [MoonSharpUserData]
    public class PlayerBan
    {
        public const int DURATION_PERMANENT = -1;
        public long DatabaseId { get; }

        [JsonIgnore]
        [MoonSharpHidden]
        public DateTime Date { get; }
        public virtual string DateStr { get { return Date.ToString(); } }

        public long Duration { get; }
        public virtual bool Expired { get { return ((Date.AddSeconds(Duration) < DateTime.Now) && Duration > 0); } }

        [JsonIgnore]
        [MoonSharpHidden]
        public BanType Type { get; }
        public virtual int TypeId { get { return (int)Type; } }

        public string PlayerName { get; }
        public string PlayerKeyhash { get; }
        public string PlayerIPAddress { get; }

        public string AdminName { get; }
        public string Reason { get; }

        public long PlayerDatabaseId { get; }
        public long AdminDatabaseId { get; }

        [MoonSharpHidden]
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
        [MoonSharpHidden]
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
        [MoonSharpHidden]
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