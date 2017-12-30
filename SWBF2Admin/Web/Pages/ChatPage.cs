using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;

using Newtonsoft.Json;

using SWBF2Admin.Runtime.Rcon;
using SWBF2Admin.Utility;
using SWBF2Admin.Structures;


namespace SWBF2Admin.Web.Pages
{
    class ChatPage : AjaxPage
    {
        private List<ChatSession> chatSessions = new List<ChatSession>();

        //Webserver is running async to main thread -> mutex is required
        private Mutex mtx = new Mutex();

        class ChatApiParams : ApiRequestParams
        {
            public string Message { get; set; }
        }

        class ChatSession
        {
            [JsonIgnore]
            public DateTime LastAcitvity { get; set; }
            [JsonIgnore]
            public Cookie ClientCookie { get; }

            public List<ChatMessage> Messages { get; }
            public ChatSession(Cookie cookie)
            {
                ClientCookie = cookie;
                Messages = new List<ChatMessage>();
            }
        }

        public ChatPage(AdminCore core) : base(core, "/live/chat", "chat.htm")
        {
            Core.Rcon.ChatInput += new EventHandler(Rcon_Chat);
            Core.Rcon.ChatOutput += new EventHandler(Rcon_Chat);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            ChatApiParams p = null;
            if ((p = TryJsonParse<ChatApiParams>(ctx, postData)) == null) return;

            if (p.Action.Equals("chat_send"))
            {
                Logger.Log(LogLevel.Verbose, "Processing webchat input: '{0}'", p.Message);
                Core.Rcon.Say(p.Message);
            }

            ChatSession s = GetSession(ctx);

            mtx.WaitOne();
            WebAdmin.SendHtml(ctx, ToJson(s.Messages));
            s.Messages.Clear();
            mtx.ReleaseMutex();
        }

        private ChatSession GetSession(HttpListenerContext ctx)
        {
            ChatSession session = null;
            Cookie cookie = null;

            mtx.WaitOne();
            foreach (Cookie c in ctx.Request.Cookies)
            {
                if (c.Name == Constants.WEB_COOKIE_CHAT)
                {
                    cookie = c;

                    foreach (ChatSession s in chatSessions)
                    {
                        if (s.ClientCookie.Equals(c))
                        {
                            session = s;
                            break;
                        }
                    }
                    break;
                }
            }

            if (session == null)
            {
                if (cookie == null)
                {
                    cookie = new Cookie(Constants.WEB_COOKIE_CHAT, Guid.NewGuid().ToString("N"));
                    Logger.Log(LogLevel.Verbose, "Setting cookie '{0}={1}'", cookie.Name, cookie.Value);
                    ctx.Response.Cookies.Add(cookie);
                }
                Logger.Log(LogLevel.Verbose, "Registering session '{0}'", cookie.Value);
                session = new ChatSession(cookie);

                //Prevent Cookie-DoS
                if (chatSessions.Count < Constants.WEB_SESSION_LIMIT)
                {
                    chatSessions.Add(session);
                }
            }

            session.LastAcitvity = DateTime.Now;
            mtx.ReleaseMutex();
            return session;
        }

        public void ManageSessions()
        {
            mtx.WaitOne();
            List<ChatSession> expiredSessions = new List<ChatSession>();
            foreach (ChatSession s in chatSessions)
            {
                if ((DateTime.Now - s.LastAcitvity).TotalSeconds > Constants.WEB_COOKIE_TIMEOUT)
                {
                    expiredSessions.Add(s);
                }
            }
            foreach (ChatSession s in expiredSessions)
            {
                chatSessions.Remove(s);
            }
            mtx.ReleaseMutex();
        }

        private void Chat_Input(ChatMessage msg)
        {
            mtx.WaitOne();
            foreach (ChatSession s in chatSessions)
            {
                s.Messages.Add(msg);
            }
            mtx.ReleaseMutex();
        }

        private void Rcon_Chat(object sender, EventArgs e)
        {
            RconChatEventArgs re = (RconChatEventArgs)e;
            Chat_Input(new ChatMessage(re.Message, re.Name));
        }
    }
}