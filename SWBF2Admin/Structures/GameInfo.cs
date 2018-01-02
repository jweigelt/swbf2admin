using System;
namespace SWBF2Admin.Structures
{
    public class GameInfo
    {
        private int team1Score, team2Score, team1Tickets, team2Tickets;
        public string Map { get; }
        public GameMode Mode { get; }
        public virtual int Team1Score { get { return team1Score; } }
        public virtual int Team2Score { get { return team2Score; } }
        public virtual int Team1Tickets { get { return team1Tickets; } }
        public virtual int Team2Tickets { get { return team2Tickets; } }
        public DateTime GameStarted { get; }
        public long DatabaseId { get; }

        public virtual byte WinningTeam
        {
            get
            {
                if (GameMode.IsScoreMode(Mode))
                {
                    if (Mode == GameMode.ASS) //quickfix for swbf2's faulty number formatting
                        return (Team1Score < Team2Score ? (byte)1 : (byte)0);
                    else
                        return (Team1Score > Team2Score ? (byte)1 : (byte)0);
                }

                else
                    return (Team1Tickets > Team2Tickets ? (byte)1 : (byte)0);
            }
        }

        public GameInfo(string map, string mode)
        {
            Mode = GameMode.GetModeByName(mode);
            Map = map;
        }

        public GameInfo(long databaseId, DateTime gameStarted, string map, string mode, int Team1Score, int Team2Score, int Team1Tickets, int Team2Tickets)
        {
            Map = map;
            Mode = GameMode.GetModeByName(mode);
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
