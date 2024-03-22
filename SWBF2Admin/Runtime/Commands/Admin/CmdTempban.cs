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
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/tempban.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdTempBan : PlayerCommand
    {
        public string OnNoTimeSpan { get; set; } = "No timespan specified. Usage: {usage}";
        public string OnInvalidTimeSpan { get; set; } = "Invalid input {input}. Expecting valid integer.";

        public string OnTempban { get; set; } = "{player} was banned for {time} by {admin}";
        public string OnTempbanReason { get; set; } = "{player} was banned for {time} by {admin} for {reason}";

        private string durationFormat = @"dd\d\ hh\h\ mm\m\ s\s";
        public string DurationFormat
        {
            get { return durationFormat; }
            set
            {
                new TimeSpan(0).ToString(value); //will throw FormatException if value is invalid
                durationFormat = value;
            }
        }
        public string DefaultReason { get; set; } = "";

        public CmdTempBan() : base("tempban", "tempban") { }

        public CmdTempBan(string alias, string permission) : base(alias, permission) { }

        public override bool AffectPlayer(Player affectedPlayer, Player player, string commandLine, string[] parameters, int paramIdx)
        {
            if (parameters.Length <= paramIdx)
            {
                SendFormatted(OnNoTimeSpan, "{usage}", Usage);
                return false;
            }

            int seconds;
            if (!int.TryParse(parameters[paramIdx], out seconds))
            {
                SendFormatted(OnInvalidTimeSpan, "{input}", parameters[paramIdx]);
                return false;
            }
            paramIdx++;

            TimeSpan duration = new TimeSpan(0, 0, seconds);

            string reason;
            if (parameters.Length > paramIdx)
            {
                reason = string.Join(" ", parameters, paramIdx, parameters.Length - paramIdx);
                SendFormatted(OnTempbanReason, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{time}", duration.ToString(), "{reason}", reason);
            }
            else
            {
                reason = DefaultReason;
                SendFormatted(OnTempban, "{player}", affectedPlayer.Name, "{admin}", player.Name, "{time}", duration.ToString(DurationFormat));
            }
            BanPlayer(affectedPlayer, player, duration, reason);
            Core.Players.Kick(affectedPlayer);
            return true;
        }

        public virtual void BanPlayer(Player p, Player admin, TimeSpan duration, string reason = "")
        {
            PlayerBan b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, admin.Name, reason, duration, BanType.Keyhash, p.DatabaseId, admin.DatabaseId);
            Core.Database.InsertBan(b);
        }
    }
}
