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