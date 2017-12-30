using System.Net;

namespace SWBF2Admin.Web.Pages
{
    class GroupSettingsPage : AjaxPage
    {
        public GroupSettingsPage(AdminCore core) : base(core, "/settings/groups", "groups.htm") { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {

        }
    }
}