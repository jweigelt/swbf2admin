using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Web.Pages;
using SWBF2Admin.Config;
using System.Diagnostics;
using System.Security.Principal;
using System.Reflection;

namespace SWBF2Admin.Web
{
    public class WebServer : ComponentBase
    {
        private const int NETSH_WAIT = 1500;
        private enum WinErrorCode
        {
            AccessDenied = 5
        }

        private List<WebPage> webpages = new List<WebPage>();
        private Dictionary<string, WebUser> authCache = new Dictionary<string, WebUser>();

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
            string rr = Assembly.GetCallingAssembly().GetType().GUID.ToString();

            try
            {
                StartListener();
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == (int)WinErrorCode.AccessDenied)
                {
                    RegisterPrefix(prefix);
                    StartListener();
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Failed to start HttpListener ({0})", e.Message);
                    throw e;
                }
            }

            running = true;
            Logger.Log(LogLevel.Info, Log.WEB_START, prefix);

            workThread = new Thread(WorkThread_Run);
            workThread.Start();
        }
        private void Stop()
        {
            running = false;
            listener.Stop();
            workThread.Join();
        }

        private void RegisterPrefix(string prefix)
        {
            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                string cmd = $"netsh http add urlacl url={prefix} user={Environment.UserDomainName}\\{Environment.UserName}";
                Logger.Log(LogLevel.Info, "Trying to register HttpPrefix \"{0}\"", prefix);

                ProcessStartInfo info = new ProcessStartInfo("cmd.exe", $"/C {cmd}");
                info.CreateNoWindow = true;
                info.UseShellExecute = true;
                info.Verb = "runas";

                Logger.Log(LogLevel.Info, "--> {0}", cmd);

                try
                {
                    Process p = Process.Start(info);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, "Failed to register HttpPrefix. ({0})", e.Message);
                    throw e;
                }

                //Windows' UAC doesn't allow the redirection of stdout when using runas
                //therefore we can only assume netsh is done after waiting a bit
                Thread.Sleep(NETSH_WAIT);
            }
            else //XP
            {
                Logger.Log(LogLevel.Info, "No UAC support found - please add urlacl \"{0}\" manually.", prefix);
            }
        }

        private void StartListener()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            listener.Start();
        }

        private void WorkThread_Run()
        {
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
            WebUser user = null;

            if (authCache.TryGetValue(identity.Name, out user))
            {
                if (!Util.Md5(identity.Password).Equals(user.PasswordHash))
                {
                    user = Core.Database.GetWebUser(identity.Name, identity.Password);
                    if (user != null)
                    {
                        authCache.Remove(user.Username);
                        authCache.Add(user.Username, user);
                    }
                }
            }
            else
            {
                user = Core.Database.GetWebUser(identity.Name, identity.Password);
                if (user != null) authCache.Add(user.Username, user);
            }
            return user;
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