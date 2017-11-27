using System.Net;
using SWBF2Admin.Gameserver;

namespace SWBF2Admin.Web.Pages
{
    class GeneralSettingsPage : AjaxPage
    {
        public GeneralSettingsPage(AdminCore core) : base(core, Constants.WEB_URL_SETTINGS_GENERAL, Constants.WEB_FILE_SETTINGS_GENERAL) { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
          
        }
    }
}