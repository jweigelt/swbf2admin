using System.Net;
using SWBF2Admin.Utility;
namespace SWBF2Admin.Web.Pages
{
    class AboutPage : AjaxPage
    {
        public AboutPage(AdminCore core) : base(core, "/web/about", "about.htm") { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx,
               "{product:name}", Util.GetProductName(),
               "{product:version}", Util.GetProductVersion(),
               "{product:author}", Util.GetProductAuthor());
        }
    }
}