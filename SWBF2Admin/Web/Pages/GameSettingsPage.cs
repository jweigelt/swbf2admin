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
using SWBF2Admin.Structures;
using SWBF2Admin.Structures.Attributes;
using SWBF2Admin.Gameserver;

namespace SWBF2Admin.Web.Pages
{
    class GameSettingsPage : AjaxPage
    {
        public GameSettingsPage(AdminCore core) : base(core, "/settings/game", "game.htm") { }

        class GameSettingsApiParams : ApiRequestParams
        {
            public ServerSettings Settings { get; set; }
        }
        class GameSettingsResponse
        {
            public ServerSettings Settings { get; }
            public GameSettingsResponse(ServerSettings settings)
            {
                Settings = settings;
            }
        }
        class GameSettingsSaveResponse
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public bool NeedsReload { get; set; }
            public GameSettingsSaveResponse(Exception e)
            {
                Ok = false;
                Error = e.Message;
            }
            public GameSettingsSaveResponse()
            {
                Ok = true;
            }
        }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            GameSettingsApiParams p = null;
            if ((p = TryJsonParse<GameSettingsApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "game_get":
                    ServerSettings s = Core.Server.Settings;
                    WebAdmin.SendHtml(ctx, ToJson(new GameSettingsResponse(s)));
                    break;

                case "game_set":
                    WebServer.LogAudit(user, "modified game settings");
                    var changes = Core.Server.Settings.UpdateFrom(p.Settings, ConfigSection.GAME);

                    if (Core.Config.EnableRuntime && Core.Server.Status == ServerStatus.Online)
                    {
                        Core.Scheduler.PushTask(() => Core.Rcon.UpdateServerSettings(changes));
                    }

                    try
                    {
                        Core.Server.Settings.WriteToFile(Core);
                        WebAdmin.SendHtml(ctx, ToJson(new GameSettingsSaveResponse()));
                    }
                    catch (Exception e)
                    {
                        WebAdmin.SendHtml(ctx, ToJson(new GameSettingsSaveResponse(e)));
                    }
                    break;
            }
        }
    }
}