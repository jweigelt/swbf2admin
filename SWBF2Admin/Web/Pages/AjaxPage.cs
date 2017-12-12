using System;
using System.Net;
using System.IO;

using Newtonsoft.Json;

using SWBF2Admin.Utility;

namespace SWBF2Admin.Web.Pages
{
    abstract class AjaxPage : WebPage
    {
        protected string Template { get; }

        public AjaxPage(AdminCore core, string url, string template) : base(core, url)
        {
            Template = template;
        }

        public void ReturnTemplate(HttpListenerContext ctx, params string[] replacements)
        {
            string html = WebAdmin.LoadTemplate(Template);

            for (uint i = 0; i < replacements.Length; i += 2)
            {
                if (replacements.Length < i)
                {
                    Logger.Log(LogLevel.Error, Log.WEB_REPLACEMENT_NOVALUE, replacements[i]);
                }
                else
                {
                    html = html.Replace(replacements[i], replacements[i + 1]);
                }
            }

            WebAdmin.SendHtml(ctx, html);
        }

        public override void HandleContext(HttpListenerContext ctx, WebUser user)
        {
            if (ctx.Request.HttpMethod == "GET")
            {
                HandleGet(ctx, user);
            }
            else if (ctx.Request.HttpMethod == "POST")
            {
                string postData = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                HandlePost(ctx, user, postData);
            }
        }

        public virtual void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }

        public virtual void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            WebAdmin.SendHtml(ctx, "Change me.");
        }

        protected T TryJsonParse<T>(HttpListenerContext ctx, string json)
        {
            T obj = default(T);
            try
            {
                obj = JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Verbose, "Invalid request: json failed ({0})", e.ToString());
                WebAdmin.SendHttpStatus(ctx, HttpStatusCode.BadRequest);
            }
            return obj;
        }

        protected string ToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

    }
}