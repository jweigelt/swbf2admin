using System.Net;
using SWBF2Admin.Utility;

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

            switch (p.Action)
            {
                case "users_get":
                    break;
                case "users_create":
                    Core.Database.InsertWebUser(new WebUser(p.Username, Util.Md5(p.SpaceInvaders)));
                    break;
                case "users_edit":
                    Core.Database.UpdateWebUser(new WebUser(p.Id, p.Username, Util.Md5(p.SpaceInvaders)), p.UpdateSpaceInvaders);
                    break;
                case "users_delete":
                    Core.Database.DeleteWebUser(new WebUser(p.Id));
                    break;
            }

            WebAdmin.SendHtml(ctx, ToJson(Core.Database.GetWebUsers()));
        }
    }
}