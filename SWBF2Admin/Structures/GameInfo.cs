using System;
namespace SWBF2Admin.Structures
{
    public class GameInfo
    {
        private int team1Score, team2Score,team1Tickets, team2Tickets;
        public string Map { get; }
        public virtual int Team1Score { get { return team1Score; } }
        public virtual int Team2Score { get { return team2Score; } }
        public virtual int Team1Tickets { get { return team1Tickets; } }
        public virtual int Team2Tickets { get { return team2Tickets; } }
        public DateTime GameStarted { get; }
        public long DatabaseId { get; }

        public GameInfo(string map)
        {
            Map = map;
        }
    
        public GameInfo(long databaseId, DateTime gameStarted, string map, int Team1Score, int Team2Score, int Team1Tickets, int Team2Tickets)
        {
            Map = map;
            DatabaseId = databaseId;
            GameStarted = GameStarted;
        }

        public void UpdateScore(ServerInfo info)
        {
            team1Score = info.Team1Score;
            team2Score = info.Team2Score;
            team1Tickets = info.Team1Tickets;
            team2Tickets = info.Team2Tickets;
        }
    }
}
