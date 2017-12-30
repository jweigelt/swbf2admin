using System;
using System.IO;
using System.Net;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Web.Pages
{
    class ResourcesPage : WebPage
    {
        public ResourcesPage(AdminCore core) : base(core, "/res") { }

        public override bool UriMatch(Uri uri)
        {
            return (uri.AbsolutePath.StartsWith(Url));
        }

        public override void HandleContext(HttpListenerContext ctx, WebUser user)
        {
            string url = ctx.Request.Url.AbsolutePath;
            byte[] buffer = null;

            Logger.Log(LogLevel.Verbose, Log.WEB_SERVE_STATIC, url);

            try
            {
                buffer = WebAdmin.LoadBinary(url);
            }
            catch (FileNotFoundException)
            {
                Logger.Log(LogLevel.Verbose, Log.WEB_STATIC_NOT_FOUND, url);
                WebAdmin.SendHttpStatus(ctx, HttpStatusCode.NotFound);
            }

            //TODO: clean that up
            if(url.EndsWith(".css")) ctx.Response.ContentType = "text/css";
            if (url.EndsWith(".js")) ctx.Response.ContentType = "text/javascript";
            if (url.EndsWith(".png")) ctx.Response.ContentType = "image/png";

            if (buffer != null) WebAdmin.SendBuffer(ctx, buffer);
        }
    }
}