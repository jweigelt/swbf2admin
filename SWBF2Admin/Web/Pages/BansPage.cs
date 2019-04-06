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
using System.Net;
using System.Collections.Generic;

using Newtonsoft.Json;

using SWBF2Admin.Structures;

namespace SWBF2Admin.Web.Pages
{
    class BansPage : AjaxPage
    {
        enum QuickBanAction
        {
            Info = 0,
            Kick = 1,
            Ban = 2,
            Swap = 3
        }

        class BanApiParams : ApiRequestParams
        {
            public string PlayerNameExp { get; set; }
            public string AdminNameExp { get; set; }
            public string ReasonExp { get; set; }
            public string StartDateStr { get; set; }
            public int DatabaseId { get; set; }

            [JsonIgnore]
            public virtual DateTime StartDate
            {
                get
                {
                    //TODO: clean that up
                    DateTime r;
                    return (DateTime.TryParse(StartDateStr, out r) ? r : new DateTime(1970, 1, 1));
                }
            }
            public bool Expired { get; set; }
            public int Type { get; set; }
            public QuickBanAction QuickBan { get; set; }
        }

        class BanAdminResponse
        {
            public bool Ok { get; }
            public BanAdminResponse(bool ok)
            {
                Ok = ok;
            }
        }

        public BansPage(AdminCore core) : base(core, "/db/bans", "bans.htm") { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            BanApiParams p = null;
            if ((p = TryJsonParse<BanApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "bans_update":
                    List<PlayerBan> banList = Core.Database.GetBans(p.PlayerNameExp, p.AdminNameExp, p.ReasonExp, p.Expired, p.Type, p.StartDate, 25);
    
                    WebAdmin.SendHtml(ctx, ToJson(banList));
                    break;

                case "bans_delete":
                    Core.Database.DeleteBan(p.DatabaseId);
                    WebAdmin.SendHtml(ctx, ToJson(new BanAdminResponse(true)));
                    break;
            }
        }
    }
}