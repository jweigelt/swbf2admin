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
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/ban.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.ban.xml"*/)]
    public class CmdBan : PlayerCommand
    {

        public string OnBan { get; set; } = "{player} was banned by {admin}";
        public string OnBanReason { get; set; } = "{player} was kicked by {admin} for {reason}";
        public CmdBan() : base("ban", "ban") { }
        public CmdBan(string n, string p) : base(n, p) { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length > paramIdx)
            {
                string reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
                SendFormatted(OnBanReason, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{reason}", reason);
            }
            else
            {
                SendFormatted(OnBan, "{player}", affectedPlayer.Name, "{admin}", player.Name);
            }

            Core.Players.Kick(affectedPlayer);
            return true;
        }

        public virtual void BanPlayer(Player p, Player admin, string reason = "")
        {
            PlayerBan b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, admin.Name, reason, BanType.Keyhash, p.DatabaseId, admin.DatabaseId);
            Core.Database.InsertBan(b);
        }
    }
}