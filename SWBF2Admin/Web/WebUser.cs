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