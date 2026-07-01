/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin).
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Net;
using SWBF2Admin.Structures;
using SWBF2Admin.Utility;
using SWBF2Admin.Structures.InGame;
namespace SWBF2Admin.Web.Pages
{
    class GameEditPage : AjaxPage
    {
        class GameEditApiParams : ApiRequestParams
        {
            public int TimeToAdd { get; set; }
            public int Team { get; set; }
            public int Score { get; set; }

            public int Slot { get; set; }
            public int Points { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int FlagCaps { get; set; }
            public int TeamKills { get; set; }
            public int TeamId { get; set; }
        }

        public GameEditPage(AdminCore core) : base(core, "/live/gameedit", "gameedit.htm") { }

        class GameEditSaveResponse
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public GameEditSaveResponse(string e)
            {
                Ok = false;
                Error = e;
            }
            public GameEditSaveResponse()
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
            GameEditApiParams p = null;
            if ((p = TryJsonParse<GameEditApiParams>(ctx, postData)) == null) return;
            if (Core.Game.LatestGame == null) { WebAdmin.SendHtml(ctx, ToJson(new GameEditSaveResponse("Latest game not active"))); return; }

            // In the game's lua scripts the mode container object is 
            // 'conquest' for Conquest
            // 'ctf' for CTF and 1Flag
            // 'hunt' for Hunt
            // unsure for hero assault space etc....
            var mode = Core.Game.LatestInfo.GameMode.ToLower();
            if (mode == null) return;

            if (mode.Contains("ctf"))
            {
                mode = "ctf";
            }
            else if (mode.Contains("hun"))
            {
                mode = "hunt";
            }
            else if (mode.Contains("con"))
            {
                mode = "conquest";
            }
            else
            {
                // Gamemode not found
                Logger.Warn("invalid game mode {0}", mode);
            }

            Player player = Core.Players.GetPlayerBySlot(p.Slot + 1);

            switch (p.Action)
            {

                case "add_time":
                    WebServer.LogAudit(user, "added {0} seconds to the mission timer", p.TimeToAdd.ToString());
                    Core.Rcon.IngameLua(string.Format("local v = GetTimerValue({0}.loseTimer) v = v+{1} SetTimerValue({0}.loseTimer,v)", mode, p.TimeToAdd)); // Add 15 seconds to the mission timer
                    Core.Rcon.Say(string.Format("Added {0} seconds to mission", p.TimeToAdd.ToString()));
                    break;
                case "add_flag":
                    WebServer.LogAudit(user, "added {0} to team {1}", p.Score.ToString(), p.Team.ToString());
                    Core.Rcon.IngameLua(string.Format("AddTeamPoints({0},{1})", p.Team, p.Score));
                    Core.Rcon.Say("Added 1 flag to team 1");
                    break;
                case "get_player_score":
                    SimpleScore score = CharacterUtils.GetCharacter(p.Slot-1, Core.BF2.reader).Score;
                    WebAdmin.SendHtml(ctx, ToJson(score));
                    return;
                case "get_char":
                    Character c = CharacterUtils.GetCharacter(p.Slot, Core.BF2.reader);
                    WebAdmin.SendHtml(ctx, ToJson(c));
                    return;
            }
            ServerInfo info = Core.Game.LatestInfo;
            WebAdmin.SendHtml(ctx, ToJson(info));
        }
    }
}
