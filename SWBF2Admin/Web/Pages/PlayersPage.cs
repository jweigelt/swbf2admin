using System.Net;
using Newtonsoft.Json;
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
            public byte PlayerId { get; set; }
            public int BanDuration { get; set; }
            public int BanTypeId { get; set; }
            [JsonIgnore]
            public virtual BanType BanType { get { return (BanType)BanTypeId; } }
        }

        class QuickAdminResponse
        {
            public bool Ok { get; set; } = true;
        }

        public PlayersPage(AdminCore core) : base(core, "/live/players", "players.htm") { }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            PlayerApiParams p = null;
            if ((p = TryJsonParse<PlayerApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "players_update":
                    WebAdmin.SendHtml(ctx, ToJson(Core.Players.GetPlayers_s()));
                    break;
                case "players_swap":
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    Core.Scheduler.PushTask(() => Core.Players.Swap(new Player(p.PlayerId)));
                    break;
                case "players_kick":
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    Core.Scheduler.PushTask(() => Core.Players.Kick(new Player(p.PlayerId)));
                    break;
                case "players_ban":
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    Player player;
                    if ((player = Core.Players.GetPlayerBySlot(p.PlayerId)) != null)
                    {
                        //TODO: figure something out for adminId
                        Core.Database.InsertBan(new PlayerBan(player.Name, player.KeyHash, player.RemoteAddress.ToString(), "WebAdmin", "WebAdmin", p.BanType, player.DatabaseId, 0));
                        Core.Scheduler.PushTask(() => Core.Players.Kick(player));
                    }
                 
                    break;
            }
        }
    }
}