using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Web.Pages;
using SWBF2Admin.Config;

namespace SWBF2Admin.Web
{
    public class WebServer : ComponentBase
    {
        private List<WebPage> webpages = new List<WebPage>();
        private string prefix = "http://localhost:8080/";
        private Thread workThread;
        private HttpListener listener;
        private bool running = false;
        private bool enabled = false;

        public WebServer(AdminCore core) : base(core) { }
        public override void Configure(CoreConfiguration config)
        {
            prefix = config.WebAdminPrefix;
            enabled = config.WebAdminEnable;
        }
        public override void OnInit()
        {
            RegisterPage<ResourcesPage>();
            RegisterPage<DefaultPage>();

            RegisterPage<DashboardPage>();
            RegisterPage<PlayersPage>();
            RegisterPage<ChatPage>();
            RegisterPage<BansPage>();

            RegisterPage<GeneralSettingsPage>();
            RegisterPage<GameSettingsPage>();
            RegisterPage<MapSettingsPage>();
            RegisterPage<GroupSettingsPage>();

            RegisterPage<AboutPage>();

            if (enabled) Start();
        }

        public override void OnDeInit()
        {
            Stop();
        }
        private void Start()
        {
            running = true;
            workThread = new Thread(WorkThread_Run);
            workThread.Start();
        }
        private void Stop()
        {
            running = false;
            listener.Stop();
            workThread.Join();
        }
        private void WorkThread_Run()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            listener.Start();

            Logger.Log(LogLevel.Info, Log.WEB_START, prefix);

            try
            {
                while (running) HandleContext(listener.GetContext());
            }
            catch (Exception e)
            {
                //catches exception when listener.Stop() is called
                if (running) throw e;
            }

            Logger.Log(LogLevel.Info, Log.WEB_STOP, prefix);
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
            return Core.Database.GetWebUser(identity.Name, identity.Password);
        }

        public void SendHttpStatus(HttpListenerContext ctx, HttpStatusCode code)
        {
            ctx.Response.StatusCode = (int)code;
            SendBuffer(ctx, new byte[] { 0x00 });
        }

        public void SendBuffer(HttpListenerContext ctx, byte[] buffer)
        {
            ctx.Response.ContentLength64 = buffer.Length;
            ctx.Response.Headers.Add("Server", "");

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
            webpages.Add((WebPage)Activator.CreateInstance(typeof(T), Core));
        }

        public string LoadTemplate(string fileName)
        {
            return Core.Files.ReadFileText(Constants.WEB_DIR_ROOT + "/" + fileName);
        }

        public byte[] LoadBinary(string fileName)
        {
            return Core.Files.ReadFileBytes(Constants.WEB_DIR_ROOT + "/" + fileName);
        }
    }
}