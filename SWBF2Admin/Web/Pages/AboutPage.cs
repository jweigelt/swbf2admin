using System.Net;
namespace SWBF2Admin.Web.Pages
{
    class AboutPage : AjaxPage
    {
        public AboutPage(AdminCore core) : base(core, Constants.WEB_URL_ABOUT, Constants.WEB_FILE_ABOUT) { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx,
                Constants.WEB_TAG_PRODUCT_NAME, Constants.PRODUCT_NAME,
                Constants.WEB_TAG_PRODUCT_VERSION, Constants.PRODUCT_VERSION,
                Constants.WEB_TAG_PRODUCT_AUTHOR, Constants.PRODUCT_AUTHOR);
        }
    }
}