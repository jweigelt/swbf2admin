using System;
using System.Text;
using System.Net;
using System.IO;

namespace SWBF2Admin.Web
{
    class WebPage
    {
        public AdminCore Core { get; }
        public virtual WebServer WebAdmin { get { return Core.WebAdmin; } }
        public string Url { get; } 

        public WebPage(AdminCore core, string url)
        {
            Core = core;
            Url = url;
        }

        public virtual void HandleContext(HttpListenerContext ctx, WebUser user)
        {
            SendHtml(ctx, "Change me, please.");
        }

        public void SendHtml(HttpListenerContext ctx, string html)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);

        }
        public void SendFile(HttpListenerResponse response, string fileName)
        {
            byte[] buffer = File.ReadAllBytes(fileName);
            response.ContentLength64 = buffer.Length;

            using (Stream stream = response.OutputStream)
            {
                stream.Write(buffer, 0, buffer.Length);
                stream.Close();
            }
        }

        public virtual bool UriMatch(Uri uri)
        {
            return (uri.AbsolutePath.Equals(Url)) ;
        }

    }
}
