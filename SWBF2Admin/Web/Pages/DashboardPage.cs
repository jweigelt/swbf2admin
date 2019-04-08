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
using System.Net;
using SWBF2Admin.Gameserver;
using SWBF2Admin.Structures;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Web.Pages
{
    class DashboardPage : AjaxPage
    {
        class DashboardApiParams : ApiRequestParams
        {
            public int NewStatusId { get; set; }
        }

        public DashboardPage(AdminCore core) : base(core, "/live/dashboard", "dashboard.htm") { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            DashboardApiParams p = null;
            if ((p = TryJsonParse<DashboardApiParams>(ctx, postData)) == null) return;


            switch (p.Action)
            {
                case "status_set":
                    if (p.NewStatusId == (int)ServerStatus.Online)
                    {
                        WebServer.LogAudit(user, "started the server");
                        Core.Server.Start();
                    }
                    else if (p.NewStatusId == (int)ServerStatus.Offline)
                    {
                        WebServer.LogAudit(user, "stopped the server");
                        Core.Server.Stop();
                    }
                    break;
            }

            ServerInfo info = Core.Game.LatestInfo;
            if (info == null) info = new ServerInfo(); //Send default if no info recieved yet
            info.Status = Core.Server.Status;
            WebAdmin.SendHtml(ctx, ToJson(info));
        }
    }
}