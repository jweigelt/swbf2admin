using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Web.Pages;

namespace SWBF2Admin.Web
{
    class WebServer
    {

        private AdminCore core;

        private List<WebPage> webpages;
        private string prefix;
        private Thread workThread;
        private HttpListener listener;
        private bool running = false;

        public WebServer(AdminCore core, string prefix)
        {
            this.core = core;
            this.prefix = prefix;

            webpages = new List<WebPage>();

            RegisterPage<ResourcesPage>();
            RegisterPage<DefaultPage>();

            RegisterPage<DashboardPage>();
            RegisterPage<PlayersPage>();
            RegisterPage<ChatPage>();
            RegisterPage<BansPage>();

            RegisterPage<GeneralSettingsPage>();
            RegisterPage<MapSettingsPage>();

            RegisterPage<AboutPage>();  
        }

        public void Start()
        {
            running = true;
            workThread = new Thread(WorkThread_Run);
            workThread.Start();
        }

        private void WorkThread_Run()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            listener.Start();

            Logger.Log(LogLevel.Info, Log.WEB_START, prefix);

            while (running) HandleContext(listener.GetContext());

            Logger.Log(LogLevel.Verbose, Log.WEB_STOP, prefix);
            listener.Stop();
        }

        private void HandleContext(HttpListenerContext ctx)
        {
            Uri url = ctx.Request.Url;
            WebPage page = null;
            WebUser user = null;

            Logger.Log(LogLevel.Verbose, Log.WEB_REQUEST, url.ToString());

            if ((user = CheckAuth(ctx)) == null)
            {
                SendHttpStatus(ctx, HttpStatusCode.Unauthorized);
                return;
            }

            foreach (WebPage p in webpages)
            {
                if (p.UriMatch(url))
                {
                    page = p;
                    break;
                }
            }

            if (page == null)
            {
                Logger.Log(LogLevel.Verbose, Log.WEB_NO_PAGE_HANDLER, url.AbsolutePath);
                SendHttpStatus(ctx, HttpStatusCode.NotFound);
            }
            else
            {
                try
                {
                    page.HandleContext(ctx, user);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, Log.WEB_UNHANDLED_ERROR, e.ToString());
                    SendHttpStatus(ctx, HttpStatusCode.InternalServerError);
                }
            }

        }

        private WebUser CheckAuth(HttpListenerContext ctx)
        {
            HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)ctx.User.Identity;
            return core.Database.GetWebUser(identity.Name, identity.Password);
        }

        public void SendHttpStatus(HttpListenerContext ctx, HttpStatusCode code)
        {
            ctx.Response.StatusCode = (int)code;
            SendBuffer(ctx, new byte[] { 0x00 });
        }

        public void SendBuffer(HttpListenerContext ctx, byte[] buffer)
        {
            ctx.Response.ContentLength64 = buffer.Length;

            using (Stream stream = ctx.Response.OutputStream)
            {
                stream.Write(buffer, 0, buffer.Length);
                stream.Close();
            }
        }

        public void SendHtml(HttpListenerContext ctx, string html)
        {
            SendBuffer(ctx, Encoding.UTF8.GetBytes(html));
        }

        private void RegisterPage<T>()
        {
            webpages.Add((WebPage)Activator.CreateInstance(typeof(T), core));
        }

        public string LoadTemplate(string fileName)
        {
            return core.Files.ReadFileText(Constants.WEB_DIR_ROOT + "/" + fileName);
        }

        public byte[] LoadBinary(string fileName)
        {
            return core.Files.ReadFileBytes(Constants.WEB_DIR_ROOT + "/" + fileName);
        }
    }
}