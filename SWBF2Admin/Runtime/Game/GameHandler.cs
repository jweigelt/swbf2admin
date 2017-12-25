using System;
using SWBF2Admin.Config;
using SWBF2Admin.Structures;
using SWBF2Admin.Runtime.Rcon.Packets;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.Game
{
    public class GameHandler : ComponentBase
    {
        public event EventHandler GameClosed;

        private GameInfo currentGame = null;
        private ServerInfo latestInfo = null;

        public virtual ServerInfo LatestInfo { get { return latestInfo; } }
        public virtual GameInfo LatestGame
        {
            get
            {
                return currentGame;
            }
        }

        GameHandlerConfiguration config;

        public GameHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<GameHandlerConfiguration>();
            UpdateInterval = this.config.StatusUpdateInterval;
        }

        public override void OnInit()
        {
            Core.Rcon.GameEnded += new EventHandler(Rcon_GameEnded);
        }

        public override void OnServerStart()
        {
            //Re-open last game (if it exists)
            currentGame = Core.Database.GetLastOpenGame();
            if (currentGame == null)
            {
                UpdateInfo();
                if (latestInfo != null) CreateNewGame(latestInfo.CurrentMap);
            }
            else Logger.Log(LogLevel.Verbose, "Found open game {0} ({1}).", currentGame.DatabaseId.ToString(), currentGame.Map);

            EnableUpdates();
            OnUpdate(); //make sure we get the first update fast
        }

        public override void OnServerStop()
        {
            DisableUpdates();
            SaveGameStats();
        }

        protected override void OnUpdate()
        {
            UpdateInfo();
        }

        private void Rcon_GameEnded(object sender, EventArgs e)
        {
            SaveGameStats();
            //Assume we're so fast that the server hasn't loaded the new map yet
            CreateNewGame(latestInfo.NextMap);
        }

        private void SaveGameStats()
        {
            if (currentGame != null)
            {
                UpdateInfo(); //make sure we save the final score/tickets
                currentGame.UpdateScore(latestInfo);

                Logger.Log(LogLevel.Verbose, "Closing game {0} ({1}). Final score: {2}/{3}",
                    currentGame.DatabaseId.ToString(),
                    currentGame.Map,
                    currentGame.Team1Score.ToString(),
                    currentGame.Team2Score.ToString());

                Core.Database.CloseGame(currentGame);
                GameClosed.Invoke(this, new GameClosedEventArgs(currentGame));
            }
        }

        private void CreateNewGame(string map)
        {
            Logger.Log(LogLevel.Verbose, "Registering new game ({0})", map);
            Core.Database.InsertGame(new GameInfo(map));
            currentGame = Core.Database.GetLastOpenGame();
        }

        private void UpdateInfo()
        {
            StatusPacket sp = new StatusPacket();
            Core.Rcon.SendPacket(sp);
            if (sp.PacketOk)
            {
                latestInfo = sp.Info;
            }
        }
    }
}