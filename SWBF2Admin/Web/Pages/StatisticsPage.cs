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
using System.Net;
using Newtonsoft.Json;
using SWBF2Admin.Structures;
using SWBF2Admin.Export;
using System;

namespace SWBF2Admin.Web.Pages
{
    class StatisticsPage : AjaxPage
    {
        class StatApiParams : ApiRequestParams
        {
            public int DatabaseId { get; set; } = 0;
            public bool Selected { get; set; } = true;
            public string NameExp { get; set; } = "";
            public string MapExp { get; set; } = "";
            public string DateFromStr { get; set; } = "";
            public string DateUntilStr { get; set; } = "";
            [JsonIgnore]
            public virtual DateTime DateFrom
            {
                get
                {
                    return (DateTime.TryParse(DateFromStr, out DateTime r) ? r : new DateTime(1970, 1, 1));
                }
            }
            [JsonIgnore]
            public virtual DateTime DateUntil
            {
                get
                {
                    return (DateTime.TryParse(DateUntilStr, out DateTime r) ? r : DateTime.Now);
                }
            }
            public int Page { get; set; } = 0;
        }

        class StatApiModifyParams
        {
            public bool Ok { get; } = true;
            public int DatabaseId { get; }
            public string Name { get; } = null;
            public bool Selected { get; } = false;
            public string Error { get; } = null;
            public StatApiModifyParams(int databaseId)
            {
                DatabaseId = databaseId;
            }
            public StatApiModifyParams(int databaseId, string name)
            {
                DatabaseId = databaseId;
                Name = name;
            }
            public StatApiModifyParams(int databaseId, bool selected)
            {
                DatabaseId = databaseId;
                Selected = selected;
            }
            public StatApiModifyParams(Exception e)
            {
                Ok = false;
                Error = e.Message;
            }
        }

        public StatisticsPage(AdminCore core) : base(core, "/db/statistics", "statistics.htm") { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            StatApiParams p = null;
            if ((p = TryJsonParse<StatApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "stats_update":
                    WebAdmin.SendHtml(ctx, ToJson(Core.Database.GetMatches(p.NameExp, p.MapExp, p.Selected, p.DateFrom, p.DateUntil, p.Page, 20)));
                    break;
                case "stats_select":
                    try
                    {
                        Core.Database.UpdateMatchSelected(p.DatabaseId, p.Selected);
                        WebAdmin.SendHtml(ctx, ToJson(new StatApiModifyParams(p.DatabaseId, p.Selected)));
                    }
                    catch (Exception e)
                    {
                        WebAdmin.SendHtml(ctx, ToJson(new StatApiModifyParams(e)));
                    }

                    break;
                case "stats_delete":
                    try
                    {
                        Core.Database.DeleteMatch(p.DatabaseId);
                        WebAdmin.SendHtml(ctx, ToJson(new StatApiModifyParams(p.DatabaseId)));
                    }
                    catch (Exception e)
                    {
                        WebAdmin.SendHtml(ctx, ToJson(new StatApiModifyParams(e)));
                    }
                    break;
                case "stats_edit":
                    try
                    {
                        Core.Database.UpdateMatchName(p.DatabaseId, p.NameExp);
                        WebAdmin.SendHtml(ctx, ToJson(new StatApiModifyParams(p.DatabaseId, p.NameExp)));
                    }
                    catch (Exception e)
                    {
                        WebAdmin.SendHtml(ctx, ToJson(new StatApiModifyParams(e)));
                    }

                    break;
                case "stats_players":
                    WebAdmin.SendHtml(ctx, ToJson(Core.Database.GetMatchPlayerStats(p.DatabaseId)));
                    break;
            }
        }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            if (!ctx.Request.QueryString.HasKeys())
                ReturnTemplate(ctx);
            else
            {
                int id = int.Parse(ctx.Request.QueryString.Get("export_id"));
                SendCSV(ctx, id);
            }
        }

        private void SendCSV(HttpListenerContext ctx, int id)
        {
            GameInfo info = Core.Database.GetMatch(id);

            string csv = StatisticsReportGenerator.GenerateReport(info, Core.Database.GetMatchPlayerStats(id));
            ctx.Response.AddHeader("Content-Disposition", $"attachment; filename=\"matchexport_{info.GameStartedStr}.csv\"");
            WebAdmin.SendHtml(ctx, csv);
        }
    }
}