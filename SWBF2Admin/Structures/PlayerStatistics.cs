using MoonSharp.Interpreter;
namespace SWBF2Admin.Structures
{
    [MoonSharpUserData]
    public class PlayerStatistics
    {
        public PlayerStatistics(int gamesWon, int gamesLost, int gamesQuit, int totalKills, int totalDeaths, int totalScore)
        {
            GamesWon = gamesWon;
            GamesLost = gamesLost;
            GamesQuit = gamesQuit;
            TotalKills = totalKills;
            TotalDeaths = totalDeaths;
            TotalScore = totalScore;
        }

        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int GamesQuit { get; set; }
        public virtual int TotalGames { get { return (GamesWon + GamesLost + GamesQuit); } }

        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalScore { get; set; }

        public virtual float TotalKDRatio { get { return (float)TotalKills / TotalDeaths; } }
        public virtual float TotalKGRatio { get { return (float)TotalKills / TotalGames; } }
        public virtual float TotalSGRatio { get { return (float)TotalScore / TotalGames; } }
        public virtual float TotalDGRatio { get { return (float)TotalDeaths / TotalGames; } }

    }
}