using System.Net;
using SWBF2Admin.Gameserver;
using SWBF2Admin.Structures;

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
                        Core.Server.Start();
                    }
                    else if (p.NewStatusId == (int)ServerStatus.Offline)
                    {
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