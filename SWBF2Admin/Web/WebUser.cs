using System;

namespace SWBF2Admin.Web
{
    class WebUser
    {
        public long Id { get; }
        public string Username { get; }
        public DateTime LastVisit { get; }

        public WebUser(long id, string username, DateTime lastVisit)
        {
            Id = id;
            Username = username;
            LastVisit = lastVisit;
        }

    }
}