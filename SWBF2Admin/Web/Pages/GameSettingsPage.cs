using System;
using System.Net;
using SWBF2Admin.Structures;
using SWBF2Admin.Structures.Attributes;

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
                    WebAdmin.SendHtml(ctx, ToJson(new GameSettingsResponse(Core.Server.Settings)));
                    break;

                case "game_set":
                    Core.Server.Settings.UpdateFrom(p.Settings, ConfigSection.GAME);
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