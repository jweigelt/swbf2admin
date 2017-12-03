using System.Net;

namespace SWBF2Admin.Web.Pages
{
    class GameSettingsPage : AjaxPage
    {
        public GameSettingsPage(AdminCore core) : base(core, Constants.WEB_URL_SETTINGS_GAME, Constants.WEB_FILE_SETTINGS_GAME) { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
           
        }
    }
}