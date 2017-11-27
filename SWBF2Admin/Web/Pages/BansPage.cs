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

            public int BanId { get; set; }
            public QuickBanAction QuickBan { get; set; }
        }

        public BansPage(AdminCore core) : base(core, Constants.WEB_URL_BANS, Constants.WEB_FILE_BANS) { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            BanApiParams p = null;
            if ((p = TryJsonParse<BanApiParams>(ctx, postData)) == null) return;

            if (p.Action == Constants.WEB_ACTION_BANS_UPDATE)
            {
                List<PlayerBan> banList = Core.Database.GetBans(p.PlayerNameExp, p.AdminNameExp, p.ReasonExp, p.Expired, p.Type, p.StartDate, 25);
                WebAdmin.SendHtml(ctx, ToJson(banList));
            }
        }

    }
}