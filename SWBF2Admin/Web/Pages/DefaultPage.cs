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
namespace SWBF2Admin.Web.Pages
{
    class DefaultPage : AjaxPage
    {
        class DefaultApiResponse
        {
            public bool Online { get; set; }
            public bool EnableRuntime { get; set; }
            public DefaultApiResponse(bool online, bool enableRuntime) { Online = online; EnableRuntime = enableRuntime; }
        }

        public DefaultPage(AdminCore core) : base(core, "/", "frame.htm") { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx,
                "{account:username}", user.Username,
                "{account:id}", user.Id.ToString(),
                "{account:lastvisit}", user.LastVisit.ToShortDateString());
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            ApiRequestParams p = null;
            if ((p = TryJsonParse<ApiRequestParams>(ctx, postData)) == null) return;

            if (p.Action.Equals("status_get"))
            {
                WebAdmin.SendHtml(ctx, ToJson(new DefaultApiResponse(Core.Server.Status == ServerStatus.Online, Core.Config.EnableRuntime)));
            }
        }

    }
}