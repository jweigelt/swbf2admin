using System.Net;
using SWBF2Admin.Gameserver;
namespace SWBF2Admin.Web.Pages
{
    class DefaultPage : AjaxPage
    {
        class DefaultApiResponse
        {
            public bool Online { get; set; }
            public DefaultApiResponse(bool online) { Online = online;  }
        }

        public DefaultPage(AdminCore core) : base(core, Constants.WEB_URL_ROOT, Constants.WEB_FILE_FRAME) { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx,
                Constants.WEB_TAG_USER_NAME, user.Username,
                Constants.WEB_TAG_USER_LASTVISIT, user.LastVisit.ToShortDateString());
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            ApiRequestParams p = null;
            if ((p = TryJsonParse<ApiRequestParams>(ctx, postData)) == null) return;

            if (p.Action == Constants.WEB_ACTION_DEFAULT_STATUS_SET)
            {
                WebAdmin.SendHtml(ctx, ToJson(new DefaultApiResponse(Core.Server.Status == ServerStatus.Online)));
            }
        }

    }
}