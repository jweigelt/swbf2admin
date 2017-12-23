using System;

namespace SWBF2Admin.Web
{
    public class WebUser
    {
        public long Id { get; }
        public string Username { get; }
        public DateTime LastVisit { get; }
        public string PasswordHash { get; }

        public WebUser(long id, string username, string passwordHash, DateTime lastVisit)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
            LastVisit = lastVisit;
        }

    }
}