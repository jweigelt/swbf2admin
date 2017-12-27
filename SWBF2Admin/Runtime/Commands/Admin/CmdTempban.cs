using System;
using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    [ConfigFileInfo(fileName: "./cfg/cmd/tempban.xml"/*, template: "SWBF2Admin.Resources.cfg.cmd.kick.xml"*/)]
    public class CmdTempban : PlayerCommand
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

        public CmdTempban() : base("tempban", "kick") { }

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
