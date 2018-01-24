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

namespace SWBF2Admin.Web
{
    public class WebUser
    {
        public long Id { get; }
        public string Username { get; }
        [JsonIgnore]
        public DateTime LastVisit { get; }

        public virtual string LastVisitStr { get { return LastVisit.ToString(); } }
        public string PasswordHash { get; }

        public WebUser(long id)
        {
            Id = id;
        }

        public WebUser(long id, string username, string passwordHash)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
        }

        public WebUser(string username, string passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
        }

        public WebUser(long id, string username, string passwordHash, DateTime lastVisit)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
            LastVisit = lastVisit;
        }

    }
}