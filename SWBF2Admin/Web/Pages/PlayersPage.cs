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
using Newtonsoft.Json;
using SWBF2Admin.Structures;
using SWBF2Admin.Structures.InGame;

namespace SWBF2Admin.Web.Pages
{
    class PlayersPage : AjaxPage
    {
        enum QuickAdminAction
        {
            Info = 0,
            Kick = 1,
            Ban = 2,
            Swap = 3,
            Points = 4
        }

        class PlayerApiParams : ApiRequestParams
        {
            public byte PlayerId { get; set; }
            public int BanDuration { get; set; }
            public int BanTypeId { get; set; }
            [JsonIgnore]
            public virtual BanType BanType { get { return (BanType)BanTypeId; } }
            public int Points { get; set; }
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
                    WebServer.LogAudit(user, "swapped slot #{0}", p.PlayerId.ToString());
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    Core.Scheduler.PushTask(() => Core.Players.Swap(new Player(p.PlayerId)));
                    break;
                case "players_kick":
                    WebServer.LogAudit(user, "kicked slot #{0}", p.PlayerId.ToString());
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    Core.Scheduler.PushTask(() => Core.Players.Kick(new Player(p.PlayerId)));
                    break;
                case "players_ban":
                    WebServer.LogAudit(user, "banned slot #{0}", p.PlayerId.ToString());
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    Player player;
                    if ((player = Core.Players.GetPlayerBySlot(p.PlayerId)) != null)
                    {
                        PlayerBan ban;
                        //TODO: figure something out for adminId
                        string adminName = "WebAdmin";
                        string reason = "WebAdmin";
                        int adminDBId = 1;

                        if(p.BanDuration < 0) {
                            ban = new PlayerBan(player.Name, player.KeyHash, player.RemoteAddress.ToString(), adminName, reason, p.BanType, player.DatabaseId, adminDBId);
                        } else
                        {
                            TimeSpan duration = new TimeSpan(0, 0, p.BanDuration);
                            ban = new PlayerBan(player.Name, player.KeyHash, player.RemoteAddressStr, adminName, reason, duration, p.BanType, player.DatabaseId, adminDBId);
                        }

                        Core.Database.InsertBan(ban);
                       
                        Core.Scheduler.PushTask(() => Core.Players.Kick(player));
                    }
                    break;

                case "players_points":
                    WebServer.LogAudit(user, "gave slot #{0} 12 points", p.PlayerId.ToString());
                    CharacterUtils.SetScore(p.PlayerId-1, p.Points, Core.BF2.reader);
                    WebAdmin.SendHtml(ctx, ToJson(new QuickAdminResponse()));
                    break;
            }
        }
    }
}