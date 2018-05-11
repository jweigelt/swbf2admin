using System.Collections.Generic;
using System.Text;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Export
{
    class StatisticsReportGenerator
    {
        public static string GenerateReport(GameInfo info, List<Player> players)
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("sep=,");
            b.AppendLine("Name, Match started, Match duration, Map, Mode, Score Team 1, Score Team 2");
            b.AppendFormat("{0},{1},{2},{3},{4},", info.Name, info.GameStartedStr, info.DurationStr, info.Map, info.Mode); 

            if (info.HasScoreMode)
                b.AppendFormat("{0},{1},", info.Team1Score, info.Team2Score);
            else
                b.AppendFormat("{0},{1},", info.Team1Tickets, info.Team2Tickets);

            b.AppendLine();
            b.AppendLine("DB ID, Name, Team, Points, Kills, Deaths, Points / min, Keyhash");

            foreach (Player p in players)
            {
                float ppm = p.Score / (float)info.Duration.TotalMinutes;
                b.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7}", p.DatabaseId.ToString(), p.Name, p.Team, p.Score.ToString(), p.Kills.ToString(), p.Deaths.ToString(), ppm.ToString().Replace(",","."), p.KeyHash);
                b.AppendLine();
            }
            return b.ToString();
        }
    }
}