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

        public override void OnServerStart(EventArgs e)
        {
            if (config.EnableGameStatsLogging)
                StatsInitGame();
            else
                UpdateInfo();

            EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();
            if (config.EnableGameStatsLogging) StatsSaveGame();
        }

        protected override void OnUpdate()
        {
            UpdateInfo();
        }
        private void Rcon_GameEnded(object sender, EventArgs e)
        {
            if (config.EnableGameStatsLogging)
            {
                StatsSaveGame();
                //Assume we're so fast that the server hasn't loaded the new map yet
                StatsCreateGame(latestInfo.NextMap, latestInfo.GameMode);
            }
        }

        private void StatsInitGame()
        {
            //Re-open last game (if it exists)
            currentGame = Core.Database.GetLastOpenGame();
            if (currentGame == null)
            {
                UpdateInfo();
                if (latestInfo != null) StatsCreateGame(latestInfo.CurrentMap, latestInfo.GameMode);
            }
            else
            {
                Logger.Log(LogLevel.Verbose, "Found open game {0} ({1}).", currentGame.DatabaseId.ToString(), currentGame.Map);
                UpdateInfo();
            }
        }
        private void StatsSaveGame()
        {
            if (currentGame != null)
            {
                UpdateInfo(); //make sure we save the final score/tickets
                currentGame.UpdateScore(latestInfo);

                Logger.Log(LogLevel.Verbose, "Closing game {0} ({1}). Final score: {2}/{3}",
                    currentGame.DatabaseId.ToString(),
                    latestInfo.CurrentMap,
                    currentGame.Team1Score.ToString(),
                    currentGame.Team2Score.ToString());

                Core.Database.CloseGame(currentGame);
                GameClosed.Invoke(this, new GameClosedEventArgs(currentGame));
            }
        }
        private void StatsCreateGame(string map, string mode)
        {
            Logger.Log(LogLevel.Verbose, "Registering new game ({0})", map);
            Core.Database.InsertGame(new GameInfo(map, mode));
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