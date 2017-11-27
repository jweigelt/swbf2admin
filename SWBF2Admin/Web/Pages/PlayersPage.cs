using System.Net;
using System.Collections.Generic;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Web.Pages
{
    class PlayersPage : AjaxPage
    {
        enum QuickAdminAction
        {
            Info = 0,
            Kick = 1,
            Ban = 2,
            Swap = 3
        }

        class PlayerApiParams : ApiRequestParams
        {
            public int PlayerId { get; set; }
            public QuickAdminAction QuickAdmin { get; set; }
            public int BanDuration { get; set; }
        }

        public PlayersPage(AdminCore core) : base(core, Constants.WEB_URL_PLAYERS, Constants.WEB_FILE_PLAYERS) { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            PlayerApiParams p = null;
            if ((p = TryJsonParse<PlayerApiParams>(ctx, postData)) == null) return;

            if (p.Action == Constants.WEB_ACTION_PLAYERS_UPDATE)
            {
                List<Player> playerList = Core.Players.GetPlayers_s();
                WebAdmin.SendHtml(ctx, ToJson(playerList));
            }
        }

    }
}