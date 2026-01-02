/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Web.Pages;
using SWBF2Admin.Config;

using SWBF2Admin.Database;

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
            RegisterPage<StatisticsPage>();

            RegisterPage<GeneralSettingsPage>();
            RegisterPage<GameSettingsPage>();
            RegisterPage<MapSettingsPage>();
            RegisterPage<GroupSettingsPage>();

            RegisterPage<WebUsersPage>();
            RegisterPage<AboutPage>();

            if (enabled) Start();
        }

        public override void OnDeInit()
        {
            Stop();
        }

        private void Start()
        {
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

            //TODO: there seems to be a bug with Dictionaries,
            //sometimes TryGet() seems to return true even though lower/upper-case does not match
            //however when trying to Remove / Get from the dict using the same string an exception is thrown
            //causing a crash. Using try-catch for now so we can stay up if this bug occurs.
            try
            {
                user = CheckAuth(ctx);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "Authcache error: {0}", e.ToString());
                SendHttpStatus(ctx, HttpStatusCode.Unauthorized);
                return;
            }

            if (user == null)
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

            if (authCache.TryGetValue(identity.Name, out WebUser user))
            {
                if (!PBKDF2.VerifyPassword(Util.Md5(identity.Password), user.PasswordHash))
                {
                    user = Core.Database.GetWebUser(identity.Name, identity.Password);
                    if (user != null)
                    {
                        authCache.Remove(user.Username);
                        authCache.Add(user.Username, user);
                        Core.Database.UpdateLastSeen(user);
                    }
                    else
                    {
                        Logger.Log(LogLevel.Info, "User {0} ({1}): invalid login (cached/password mismatch)", identity.Name, ctx.Request.RemoteEndPoint.ToString());
                    }
                }
            }
            else
            {
                user = Core.Database.GetWebUser(identity.Name, Util.Md5(identity.Password));

                if (user != null)
                {
                    authCache.Add(user.Username, user);
                    Core.Database.UpdateLastSeen(user);
                }
                else
                {
                    Logger.Log(LogLevel.Info, "User {0} ({1}): invalid login (db/password mismatch)", identity.Name, ctx.Request.RemoteEndPoint.ToString());
                }
            }

            if (user != null)
            {
                user.IPEP = ctx.Request.RemoteEndPoint;
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
                try
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Verbose,
                        "http write error: couldn't send to {0} - {1}",
                        ctx.Request.RemoteEndPoint.ToString(), e.ToString());
                }
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

        public static void LogAudit(WebUser user, string message, params string[] p)
        {
            Logger.Log(LogLevel.Info, "[AUDIT] {0} ({1}) {2}", user.Username, user.IPEP.ToString(), string.Format(message, p));
        }
    }
}