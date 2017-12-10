using System.Net;

namespace SWBF2Admin.Web.Pages
{
    class GroupSettingsPage : AjaxPage
    {
        public GroupSettingsPage(AdminCore core) : base(core, Constants.WEB_URL_SETTINGS_GROUP, Constants.WEB_FILE_SETTINGS_GROUP) { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {

        }
    }
}