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

        public override void OnServerStart()
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
                            HandlePlayerDb(p);
                            OnNewPlayerJoin(p);
                        }
                        else
                        {
                            p.CopyDbInfo(oldPlayer);
                        }

                        if (p.IsBanned) Kick(p);
                    }
                    else
                    {
                        HandlePlayerDb(p);
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

        private void HandlePlayerDb(Player p)
        {
            if (Core.Database.PlayerExists(p))
                Core.Database.UpdatePlayer(p);
            else
                Core.Database.InsertPlayer(p);

            Core.Database.AttachDbInfo(p);
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
        }
        private void OnPlayerLeave(Player p)
        {
            Logger.Log(LogLevel.Verbose, "Player {0} left.", p.Name);
            Core.Database.AddPlayerStats(p, Core.Game.LatestGame, true);
        }

        private void Game_GameClosed(object sender, EventArgs e)
        {
            foreach (Player p in playerList)
            {
                GameClosedEventArgs gce = (GameClosedEventArgs)e;
                Core.Database.AddPlayerStats(p, gce.Game);
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

        private bool PlayerExists(Player player, List<Player> list)
        {
            foreach (Player p in list)
            {
                if (p.KeyHash.Equals(player.KeyHash)) return true;
            }
            return false;
        }
        private Player GetPlayerByKeyHash(string kh)
        {
            foreach (Player p in playerList)
            {
                if (p.KeyHash.Equals(kh)) return p;
            }
            return null;
        }

        public void Kick(Player p)
        {
            Core.Rcon.SendCommand("boot", p.Slot.ToString());
        }
        public void Swap(Player p)
        {
            Core.Rcon.SendCommand("swap", p.Slot.ToString());
        }
    }
}