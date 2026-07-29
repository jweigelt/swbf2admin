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
using SWBF2Admin.Database;
using System.Net;

namespace SWBF2Admin.Web.Pages
{
    class WebUsersPage : AjaxPage
    {
        public WebUsersPage(AdminCore core) : base(core, "/web/users", "users.htm") { }

        class WebUserApiParams : ApiRequestParams
        {
            public long Id { get; set; }
            public string SpaceInvaders { get; set; }
            public bool UpdateSpaceInvaders { get; set; }
            public string Username { get; set; }
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            WebUserApiParams p = null;
            if ((p = TryJsonParse<WebUserApiParams>(ctx, postData)) == null) return;
            bool clearAuthCache = false;

            switch (p.Action)
            {
                case "users_get":
                    break;
                case "users_create":
                    WebServer.LogAudit(user, "created user {0}", p.Username);
                    Core.Database.InsertWebUser(new WebUser(p.Username, PBKDF2.HashPassword(p.SpaceInvaders)));
                    clearAuthCache = true;
                    break;
                case "users_edit":
                    WebServer.LogAudit(user, "modified user {0}", p.Username);
                    string passwordHash = p.UpdateSpaceInvaders
                        ? PBKDF2.HashPassword(p.SpaceInvaders)
                        : null;
                    Core.Database.UpdateWebUser(
                        new WebUser(p.Id, p.Username, passwordHash), p.UpdateSpaceInvaders);
                    clearAuthCache = true;
                    break;
                case "users_delete":
                    WebServer.LogAudit(user, "deleted user {0}", p.Username);
                    Core.Database.DeleteWebUser(new WebUser(p.Id));
                    clearAuthCache = true;
                    break;
            }

            if (clearAuthCache) WebAdmin.ClearAuthCache();
            WebAdmin.SendHtml(ctx, ToJson(Core.Database.GetWebUsers()));
        }
    }
}
