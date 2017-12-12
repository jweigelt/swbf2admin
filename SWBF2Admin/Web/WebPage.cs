using System;
using System.Net;

namespace SWBF2Admin.Web
{
    abstract class WebPage
    {
        protected AdminCore Core { get; }
        protected virtual WebServer WebAdmin { get { return Core.WebAdmin; } }
        protected string Url { get; } 

        public WebPage(AdminCore core, string url)
        {
            Core = core;
            Url = url;
        }

        public abstract void HandleContext(HttpListenerContext ctx, WebUser user);

        public virtual bool UriMatch(Uri uri)
        {
            return (uri.AbsolutePath.Equals(Url)) ;
        }

    }
}