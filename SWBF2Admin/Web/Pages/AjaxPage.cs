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
using System.Net;
using System.IO;


using Newtonsoft.Json;

using SWBF2Admin.Utility;

namespace SWBF2Admin.Web.Pages
{
    abstract class AjaxPage : WebPage
    {
        protected static JsonSerializerSettings jsonEncodeSettings = new JsonSerializerSettings
        {
            ContractResolver = new EncodeHtmlResolver(),
            Formatting = Formatting.Indented
        };

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
            return JsonConvert.SerializeObject(obj, jsonEncodeSettings);
        }
    }
}