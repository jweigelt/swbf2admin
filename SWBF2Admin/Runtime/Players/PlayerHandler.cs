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
using System.Collections.Generic;
using SWBF2Admin.Config;
using SWBF2Admin.Runtime.Rcon.Packets;
using SWBF2Admin.Structures;
using SWBF2Admin.Runtime.Game;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.Players
{
    public class PlayerHandler : ComponentBase
    {
        /// <summary>
        /// List of all players currently connected to the server
        /// </summary>
        public virtual List<Player> PlayerList { get { return playerList; } }

        private List<Player> playerList;
        private PlayerHandlerConfiguration config;

        public PlayerHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<PlayerHandlerConfiguration>();
            UpdateInterval = this.config.PlayersUpdateInterval;
        }
        public override void OnInit()
        {
            Core.Game.GameClosed += new EventHandler(Game_GameClosed);
        }

        public override void OnServerStart(EventArgs e)
        {
            EnableUpdates();
        }
        public override void OnServerStop()
        {
            DisableUpdates();
        }
        protected override void OnUpdate()
        {
            PlayerListPacket plp = new PlayerListPacket();
            Core.Rcon.SendPacket(plp);

            if (plp.PacketOk)
            {
                foreach (Player p in plp.PlayerList)
                {
                    if (playerList != null)
                    {
                        Player oldPlayer = GetPlayerByKeyHash(p.KeyHash);
                        if (oldPlayer == null)
                        {
                            SetupPlayer(p);
                            OnNewPlayerJoin(p);
                        }
                        else
                        {
                            p.ProcessPrevious(oldPlayer);
                            foreach (ConditionalMessage msg in config.ConditionalMessages) msg.TickPlayer(p, Core);
                        }

                        if (p.IsBanned) Kick(p);
                    }
                    else
                    {
                        SetupPlayer(p);
                    }
                }

                if (playerList != null)
                {
                    foreach (Player p in playerList)
                    {
                        if (!PlayerExists(p, plp.PlayerList)) OnPlayerLeave(p);
                    }
                }

                playerList = plp.PlayerList;
            }
        }

        /// <summary>
        /// Handles player's db data sets, attaches dbinfo to Player object 
        /// </summary>
        /// <param name="p"></param>
        private void SetupPlayer(Player p)
        {
            if (Core.Database.PlayerExists(p))
                Core.Database.UpdatePlayer(p);
            else
                Core.Database.InsertPlayer(p);

            Core.Database.AttachDbInfo(p);
            p.MainGroup = Core.Database.GetTopGroup(p);

            p.MessageStates = new Dictionary<ConditionalMessage, bool>();
            foreach (ConditionalMessage msg in config.ConditionalMessages)
                p.MessageStates.Add(msg, false);

            p.ResetConditionTracker();
        }

        /// <summary>
        /// Called when a new player joins the game
        /// </summary>
        /// <param name="p"></param>
        private void OnNewPlayerJoin(Player p)
        {
            Logger.Log(LogLevel.Verbose, "Player {0} joined.", p.Name);
            if (p.IsBanned)
            {
                SendFormatted(config.OnPlayerAutoKickBanned, "{player}", p.Name);
            }
            else
            {
                if ((p.MainGroup != null) && p.MainGroup.EnableWelcome)
                {
                    Core.Rcon.Say(Util.FormatString(
                      ((p.TotalVisits == 1) ? p.MainGroup.NewWelcomeMessage : p.MainGroup.WelcomeMessage),
                     "{player}", p.Name,
                     "{group}", p.MainGroup.Name,
                     "{joined}", p.TotalVisits.ToString(),
                     "{id}", p.DatabaseId.ToString()));
                }
            }
        }

        /// <summary>
        /// Called when a player leaves the server
        /// </summary>
        /// <param name="p"></param>
        private void OnPlayerLeave(Player p)
        {
            Logger.Log(LogLevel.Verbose, "Player {0} left.", p.Name);
            if (Core.Game.LatestGame != null)
            {
                Core.Database.InsertPlayerStats(p, Core.Game.LatestGame, true);
            }
        }

        /// <summary>
        /// Called when GameHandler writes stats
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Game_GameClosed(object sender, EventArgs e)
        {
            if (playerList != null)
            {
                foreach (Player p in playerList)
                {
                    GameClosedEventArgs gce = (GameClosedEventArgs)e;
                    Core.Database.InsertPlayerStats(p, gce.Game);
                }
            }
        }

        /// <summary>
        /// Gets a copy of the current playerlist
        /// </summary>
        public List<Player> GetPlayers_s()
        {
            return new List<Player>(playerList);
        }

        /// <summary>
        /// Gets a List of all players who match the given condition
        /// </summary>
        /// <param name="exp">Expression to compare the player's name against</param>
        /// <param name="ignoreCase">Ignore case string comparision</param>
        /// <param name="exact">Only search players if their name equals the given expression</param>
        /// <returns>A List containing all matching players. If no players match an empty List is returned.</returns>
        public List<Player> GetPlayers(string exp, bool ignoreCase = true, bool exact = false)
        {
            List<Player> matching = new List<Player>();

            if (playerList != null)
            {
                foreach (Player p in playerList)
                {
                    if ((!exact && ignoreCase && p.Name.ToLower().Contains(exp.ToLower())) ||
                        (!exact && p.Name.Contains(exp)) ||
                        (p.Name.Equals(exp)))
                    {
                        matching.Add(p);
                    }
                }
            }
            return matching;
        }

        /// <summary>
        /// Checks if a player is present in a list of player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="list"></param>
        /// <returns>true if players is contained, false if not</returns>
        private bool PlayerExists(Player player, List<Player> list)
        {
            foreach (Player p in list)
            {
                if (p.KeyHash.Equals(player.KeyHash)) return true;
            }
            return false;
        }

        /// <summary>
        /// Searches the current playerlist for a player with matching keyhash
        /// </summary>
        /// <param name="kh"></param>
        /// <returns>Matching player object if player was found, null if no player was found</returns>
        public Player GetPlayerByKeyHash(string kh)
        {
            foreach (Player p in playerList)
            {
                if (p.KeyHash.Equals(kh)) return p;
            }
            return null;
        }

        public Player GetPlayerBySlot(byte slot)
        {
            foreach (Player p in playerList)
            {
                if (p.Slot == slot) return p;
            }
            return null;
        }

        /// <summary>
        /// Kicks a player from the server
        /// </summary>
        /// <param name="p"></param>
        public void Kick(Player p)
        {
            Core.Rcon.SendCommand("boot", p.Slot.ToString());
        }

        /// <summary>
        /// Changes a player's team
        /// </summary>
        /// <param name="p"></param>
        public void Swap(Player p)
        {
            Core.Rcon.SendCommand("swap", p.Slot.ToString());
        }
    }
}