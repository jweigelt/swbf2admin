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
using System.Data;
using System.Data.Common;

using System.Collections.Generic;
using System.Data.SQLite;

using MySql.Data.MySqlClient;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;
using SWBF2Admin.Web;
using SWBF2Admin.Config;
using SWBF2Admin.Runtime.Permissions;

namespace SWBF2Admin.Database
{
    public enum DbType { SQLite, MySQL }
    public class SQLHandler : ComponentBase
    {
        public DbType SQLType { get; set; } = DbType.SQLite;

        public string SQLTablePrefix { get; set; } = "swbf_";
        public string SQLiteFileName { get; set; } = "SWBF2Admin.sqlite";
        public string MySQLHost { get; set; } = "localhost:3306";
        public string MySQLUser { get; set; } = "root";
        public string MySQLPassword { get; set; } = "";
        public string MySQLDatabase { get; set; } = "swbf2";
        private DbConnection connection = null;

        public SQLHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            SQLType = config.SQLType;
            SQLiteFileName = Core.Files.ParseFileName(config.SQLiteFileName);
            MySQLHost = config.MySQLHostname;
            MySQLDatabase = config.MySQLDatabaseName;
            MySQLUser = config.MySQLUsername;
            MySQLPassword = config.MySQLPassword;
        }
        public override void OnInit()
        {
            Open();
        }
        public override void OnDeInit()
        {
            Close();
        }

        public bool Open()
        {
            if (connection != null)
            {
                Logger.Log(LogLevel.Warning, "[SQL] Database connection already open");
                return true;
            }

            Logger.Log(LogLevel.Verbose, "[SQL] Opening database connection...");

            if (SQLType == DbType.SQLite)
                connection = new SQLiteConnection(string.Format("Data Source={0};", SQLiteFileName));
            else
                connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3}", MySQLHost, MySQLDatabase, MySQLUser, MySQLPassword));

            try
            {
                connection.Open();
                Logger.Log(LogLevel.Info, "[SQL] Database OK.");
                return true;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "[SQL] Couldn't open database : {0}", e.Message);
                return false;
            }

        }
        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
                connection = null;
                Logger.Log(LogLevel.Info, "[SQL] Database closed.");
            }
        }
        private DbDataReader Query(string query, params object[] parameters)
        {
            Logger.Log(LogLevel.VerboseSQL, "[SQL] Query: {0}", query);
            DbDataReader reader = null;
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                reader = BuildCommand(query, parameters).ExecuteReader();
                return reader;
            }
            catch (Exception e)
            {
                if (reader != null)
                {
                    if (!reader.IsClosed) reader.Close();
                }

                Logger.Log(LogLevel.Error, "[SQL] Query failed : {0}", e.Message);
                return null;
            }
        }
        private void NonQuery(string query, params object[] parameters)
        {
            Logger.Log(LogLevel.VerboseSQL, "[SQL] NonQuery: {0}", query);
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                BuildCommand(query, parameters).ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "[SQL] Query failed : {0}", e.Message);
            }
        }
        private object ScalarQuery(string query, params object[] parameters)
        {
            Logger.Log(LogLevel.VerboseSQL, "[SQL] ScalarQuery: {0}", query);
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                return BuildCommand(query, parameters).ExecuteScalar();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "[SQL] Query failed : {0}", e.Message);
                return null;
            }
        }

        private DbCommand BuildCommand(string query, params object[] parameters)
        {
            query = query.Replace("prefix_", SQLTablePrefix);
            DbCommand command;
            if (SQLType == DbType.SQLite)
                command = new SQLiteCommand(query, (SQLiteConnection)connection);
            else
                command = new MySqlCommand(query, (MySqlConnection)connection);

            for (int i = 0; i < parameters.Length; i += 2)
            {
                if (parameters.Length < i)
                {
                    Logger.Log(LogLevel.Error, "[SQL] No value for parameter '{0}' specified", parameters[i].ToString());
                }
                else
                {
                    DbParameter p = command.CreateParameter();
                    p.ParameterName = parameters[i].ToString();
                    p.Value = parameters[i + 1];

                    command.Parameters.Add(p);
                }
            }
            command.Prepare();
            return command;
        }

        #region Setup

        private void VerifyDatabase()
        {

        }

        private void CheckTable()
        {

        }

        #endregion



        #region Util
        private bool HasRows(DbDataReader reader)
        {
            if (reader == null) return false;
            bool r = reader.HasRows;
            reader.Dispose();
            return r;
        }
        private long GetTimestamp()
        {
            return (long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        private uint GetTimestamp(DateTime date)
        {
            return (uint)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        private DateTime GetDateTime(uint timestamp)
        {
            DateTime r = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            return r.AddSeconds(timestamp).ToLocalTime();
        }
        private string RS(DbDataReader reader, string field)
        {
            return (DBNull.Value.Equals(reader[field]) ? string.Empty : (string)reader[field]);
        }
        private uint RU(DbDataReader reader, string field)
        {
            if (DBNull.Value.Equals(reader[field])) return 0;
            if (SQLType == DbType.SQLite) return (uint)(long)reader[field];
            else return (uint)reader.GetInt32(reader.GetOrdinal(field));
        }
        private int RI(DbDataReader reader, string field)
        {
            if (DBNull.Value.Equals(reader[field])) return 0;
            if (SQLType == DbType.SQLite) return (int)(long)reader[field];
            else return reader.GetInt32(reader.GetOrdinal(field));
        }

        private long RL(DbDataReader reader, string field)
        {
            if (DBNull.Value.Equals(reader[field])) return 0;
            if (SQLType == DbType.SQLite) return (long)reader[field];
            else return reader.GetInt32(reader.GetOrdinal(field));
        }
        private long LastInsertId()
        {
            string sql = (SQLType == DbType.SQLite ? "SELECT last_insert_rowid()" : "SELECT last_insert_id()");
            object ro = ScalarQuery(sql);
            if (ro != null)
            {
                return (long)ro;
            }
            return -1;
        }
        #endregion

        #region permissions
        /*
        public bool HasPermission(Player player, Permission permission)
        {
            string sql =
                "SELECT " +
                "prefix_players.id " +
                "FROM " +
                "prefix_permissions ," +
                "prefix_players " +
                "INNER JOIN prefix_players_groups ON prefix_permissions.group_id = prefix_players_groups.group_id AND prefix_players_groups.player_id = prefix_players.id " +
                "WHERE prefix_players.keyhash = @keyhash AND prefix_permissions.permission_name = @permission";

            // Permission.ToString currently returns Permission.Name, but if we used a simple enum then it would return the enum name
            return (HasRows(Query(sql, "@steam_id", player.KeyHash, "@permission", permission.ToString())));
        }
        */
        /*
         * TODO This proposition invalidates existing permission code, so just commenting most of it out
         * Proposed schema for permissions:
         * permissions:
         *     - id (int)
         *     - name (string)
         * groups:
         *     - id (int)
         *     - name (string)
         * grouped_permissions (many-to-many join table):
         *     - group_id (int, reference to groups)
         *     - permission_id (int, reference to permissions)
         * players_permissions (one-to-many joins to groups and/or permissions:
         *     - player_id: (int)
         *     - group_id (int, nullable, reference to groups)
         *     - permission_id (int, nullable, reference to permissions)
         */
        /*
         * Created some views with slightly modified schema
         * 
         * (players: all unique players)
         * (groups: player groups for #putgroup)
         * (groups_players: refs players to groups)
         * 
         * permissions_groups: group_id, permission_id refs player groups to permissions
         * permissions_players: player_id, permission_id refs players to permissions
         * 
         * permissions: id, name
         * permissions_grouped: child_id, parent_id refs permissions back to permissions, so that one permission can act as an alias for n permission
         * 
         * view_permissions_groups: selects permissions union child permissions (based on player's groups)
         * view_permissions_players: selects permissions union child permissions (based on player)
         * 
         * view_permission(player_id, permission_name): view_permissions_groups union view_permissions_players
         * 
         */
        public bool HasPermission(Player player, Permission permission)
        {
            /*
            string sql = @"
            SElECT player_id
            FROM players_permissions
                INNER JOIN players ON
                    players.id = players_permissions.player_id
                LEFT JOIN grouped_permissions ON
                    players_permissions.group_id = grouped_permissions.group_id
                INNER JOIN permissions ON
                    permissions.id = players_permissions.permission_id OR
                    permissions.id = grouped_permissions.permission_id 
            WHERE players.keyhash = @keyhash AND permissions.name = @permission_name
            ";
            */
            string sql = "SELECT player_id FROM view_permissions WHERE permission_name = @permission_name AND player_id = @player_id";
            return HasRows(Query(sql, "@player_id", player.DatabaseId, "@permission_name", permission.Name));
        }

        public IDictionary<int, Permission> GetPermissions()
        {
            string sql = "SELECT id, permission_name FROM prefix_permissions";
            IDictionary<int, Permission> permissions = new Dictionary<int, Permission>();
            using (DbDataReader reader = Query(sql))
            {
                while (reader.Read())
                {
                    Permission p = new Permission(RI(reader, "id"), RS(reader, "permission_name"));
                    permissions.Add(p.Id, p);
                }
            }
            Permission.InitPermissions(permissions);
            return permissions;
        }

        // TODO
        public IDictionary<string, PermissionGroup> GetPermissionGroups()
        {
            GetPermissions();
            string sql =
                "SELECT id, group_level, group_welcome, group_welcome_enable, group_default, group_name FROM prefix_groups";
            IDictionary<string, PermissionGroup> permissionGroups = new Dictionary<string, PermissionGroup>();
            using (DbDataReader reader = Query(sql))
            {
                while (reader.Read())
                {
                    string groupName = RS(reader, "group_name");
                    ISet<Permission> permissionsInGroup = new HashSet<Permission>();
                    foreach (KeyValuePair<int, Permission> permission in Permission.GetPermissions())
                    {
                        //                        if ()
                    }
                    //                    permissionGroups[groupName] = new PermissionGroup(groupName, );
                }
            }
            return permissionGroups;
        }

        //TODO: cache groups
        public PlayerGroup GetPlayerGroup(string name)
        {
            string sql = "SELECT * FROM prefix_groups WHERE lower(group_name) = @group_name LIMIT 1";

            using (DbDataReader reader = Query(sql, "@group_name", name.ToLower()))
            {
                if (reader.Read())
                {
                    return new PlayerGroup(
                        RL(reader, "id"),
                        RL(reader, "group_level"),
                        RS(reader, "group_name"),
                        RS(reader, "group_welcome"),
                         RS(reader, "group_welcome_new"),
                        (RL(reader, "group_welcome_enable") == 1));
                }
            }
            return null;
        }

        public PlayerGroup GetTopGroup()
        {
            string sql = "SELECT * FROM prefix_groups ORDER BY group_level DESC LIMIT 1";

            using (DbDataReader reader = Query(sql))
            {
                if (reader.Read())
                {
                    return new PlayerGroup(
                        RL(reader, "id"),
                        RL(reader, "group_level"),
                        RS(reader, "group_name"),
                        RS(reader, "group_welcome"),
                        RS(reader, "group_welcome_new"),
                        (RL(reader, "group_welcome_enable") == 1));

                }
            }
            return null;
        }

        private PlayerGroup ReadGroup(DbDataReader reader)
        {
            if (reader.Read())
            {
                return new PlayerGroup(
                     RL(reader, "id"),
                     RL(reader, "group_level"),
                     RS(reader, "group_name"),
                     RS(reader, "group_welcome"),
                     RS(reader, "group_welcome_new"),
                     (RL(reader, "group_welcome_enable") == 1));
            }
            return null;
        }

        private PlayerGroup GetDefaultGroup()
        {
            string sql =
               "SELECT * " +
               "FROM " +
                   "prefix_groups " +
               "WHERE group_default = 1";

            using (DbDataReader reader = Query(sql))
            {
                return ReadGroup(reader);
            }

        }

        public PlayerGroup GetTopGroup(Player player)
        {
            string sql =
                "SELECT * " +
                "FROM " +
                    "prefix_players_groups " +
                "INNER JOIN prefix_groups ON prefix_players_groups.group_id = prefix_groups.id " +
                "WHERE player_id = @player_id " +
                "ORDER BY group_level DESC LIMIT 1";

            using (DbDataReader reader = Query(sql, "@player_id", player.DatabaseId))
            {
                PlayerGroup group = ReadGroup(reader);
                if (group == null)
                {
                    reader.Close();
                    PlayerGroup defaultGroup = GetDefaultGroup();
                    if (defaultGroup != null) AddPlayerGroup(player, defaultGroup);
                    return defaultGroup;
                }
                return group;
            }
        }

        public bool GroupEmpty(PlayerGroup group)
        {
            string sql = "SELECT id FROM prefix_players_groups where group_id = @group_id";
            return !(HasRows(Query(sql, "@group_id", group.Id)));
        }

        public bool IsGroupMember(Player player, PlayerGroup group)
        {
            string sql = "SELECT id FROM prefix_players_groups WHERE player_id = @player_id AND group_id = @group_id";
            return (HasRows(Query(sql, "@player_id", player.DatabaseId, "@group_id", group.Id)));
        }

        public void AddPlayerGroup(Player player, PlayerGroup group)
        {
            string sql = "INSERT INTO prefix_players_groups " +
                "(player_id, group_id) VALUES " +
                "(@player_id, @group_id)";

            NonQuery(sql, "@player_id", player.DatabaseId, "@group_id", group.Id);
            player.MainGroup = GetTopGroup(player);
        }

        public void RemovePlayerGroup(Player player, PlayerGroup group)
        {
            string sql = "DELETE FROM prefix_players_groups WHERE player_id = @player_id AND group_id = @group_id";
            NonQuery(sql, "@player_id", player.DatabaseId, "@group_id", group.Id);
            player.MainGroup = GetTopGroup(player);
        }
        #endregion

        #region Ban management
        public bool IsBanned(Player player)
        {
            string sql =
                "SELECT " +
                    "prefix_bans.id " +
                "FROM " +
                    "prefix_bans " +
                "INNER JOIN swbf_players ON prefix_bans.player_id = prefix_players.id " +
                "WHERE " +
                "((player_keyhash = @keyhash AND ban_type = " + ((int)BanType.Keyhash).ToString() + ") " +
                "OR (player_last_ip = @ip AND ban_type = " + ((int)BanType.IPAddress).ToString() + ")) " +
                "AND ((ban_timestamp + ban_duration) > @timestamp OR ban_duration < 0)";

            return (HasRows(Query(sql, "@keyhash", player.KeyHash, "@ip", player.RemoteAddressStr, "@timestamp", GetTimestamp())));
        }
        public void InsertBan(PlayerBan ban)
        {
            string sql =
            "INSERT INTO prefix_bans " +
            "(player_id, admin_id, ban_reason, ban_duration, ban_type, ban_timestamp) VALUES " +
            "(@player_id, @admin_id, @ban_reason, @ban_duration, @ban_type, @ban_timestamp)";

            NonQuery(sql,
                "@player_id", ban.PlayerDatabaseId.ToString(),
                "@admin_id", ban.AdminDatabaseId.ToString(),
                "@ban_reason", ban.Reason,
                "@ban_duration", ban.Duration.ToString(),
                "@ban_type", ban.TypeId.ToString(),
                "@ban_timestamp", GetTimestamp().ToString());
        }
        public List<PlayerBan> GetBans(string playerExp, string adminExp, string reasonExp, bool expired, int banType, uint timestamp, int maxRows)
        {
            playerExp += "%";
            adminExp += "%";
            reasonExp += "%";

            string sql =
                "SELECT " +
                    "prefix_bans.id, " +
                    "ban_reason, " +
                    "ban_duration, " +
                    "ban_type, " +
                    "ban_timestamp, " +
                    "admin_id, " +
                    "player_id, " +
                    "prefix_players.player_last_name, " +
                    // If banned by the supseruser, swbf_players_admins.player_last_name will be null
                    "COALESCE(swbf_players_admins.player_last_name, 'superuser') AS admin_last_name," +
                    "prefix_players.player_keyhash, " +
                    "prefix_players.player_last_ip " +
                "FROM " +
                    "prefix_bans " +
                    "LEFT JOIN prefix_players ON prefix_players.id = prefix_bans.player_id " +
                    "LEFT JOIN prefix_players AS prefix_players_admins ON prefix_players_admins.id = prefix_bans.admin_id " +
                "WHERE " +
                    "prefix_players.player_last_name LIKE @player_exp AND " +
                    // Banned by either a player or the superuser
                    "(prefix_players_admins.player_last_name LIKE @admin_exp OR swbf_bans.admin_id=@superuser_id) AND " +
                    "ban_reason LIKE @reason_exp AND " +
                    "(ban_timestamp) > @date_timestamp";

            if (!expired) sql += " AND ((ban_timestamp + ban_duration) > @timestamp OR ban_duration < 0)";
            if (banType != (int)BanType.ShowAll) sql += " AND ban_type = @ban_type";

            sql += " ORDER BY ban_timestamp LIMIT @max_rows";


            List<PlayerBan> bans = new List<PlayerBan>();
            using (DbDataReader reader = Query(sql,
                "@player_exp", playerExp,
                "@admin_exp", adminExp,
                "@superuser_id", Player.SUPERUSER.DatabaseId,
                "@reason_exp", reasonExp,
                "@timestamp", GetTimestamp(),
                "@max_rows", maxRows,
                "@date_timestamp", timestamp,
                "@ban_type", banType))
            {
                while (reader.Read())
                {
                    bans.Add(new PlayerBan(
                        RL(reader, "id"),
                        RS(reader, "player_last_name"),
                        RS(reader, "player_keyhash"),
                        RS(reader, "player_last_ip"),
                        RS(reader, "admin_last_name"),
                        RS(reader, "ban_reason"),
                        GetDateTime(RU(reader, "ban_timestamp")),
                        RL(reader, "ban_duration"),
                        (BanType)(RU(reader, "ban_type")),
                        RL(reader, "player_id"),
                        RL(reader, "admin_id")));
                }
            }
            return bans;
        }
        public List<PlayerBan> GetBans(string playerExp, string adminExp, string reasonExp, bool expired, int banType, DateTime date, int maxRows)
        {
            return GetBans(playerExp, adminExp, reasonExp, expired, banType, GetTimestamp(date), maxRows);
        }
        public void DeleteBan(int id)
        {
            string sql =
                "DELETE FROM prefix_bans " +
                "WHERE id = @id";
            NonQuery(sql, "@id", id.ToString());
        }
        #endregion

        #region player/statistics tracking
        public bool PlayerExists(Player player)
        {
            string sql =
                "SELECT " +
                "id " +
                "FROM " +
                "prefix_players " +
                "WHERE player_keyhash = @keyhash";

            return (HasRows(Query(sql, "@keyhash", player.KeyHash)));
        }

        public void InsertPlayer(Player player)
        {
            string sql =
                "INSERT INTO prefix_players " +
                "(player_keyhash, player_last_visit, player_first_visit, player_visits, player_last_ip, player_last_name) VALUES " +
                "(@keyhash, @last_visit, @first_visit, 1, @ip, @name)";

            NonQuery(sql,
                "@keyhash", player.KeyHash,
                "@last_visit", GetTimestamp().ToString(),
                "@first_visit", GetTimestamp().ToString(),
                "@ip", player.RemoteAddressStr,
                "@name", player.Name);
        }

        public void UpdatePlayer(Player player)
        {
            string sql =
                "UPDATE prefix_players SET " +
                    "player_last_visit = @last_visit, " +
                    "player_visits = player_visits+1, " +
                    "player_last_ip = @ip, " +
                    "player_last_name = @name " +
                "WHERE player_keyhash = @keyhash";

            NonQuery(sql,
                "@last_visit", GetTimestamp().ToString(),
                "@ip", player.RemoteAddressStr,
                "@name", player.Name,
                "@keyhash", player.KeyHash);
        }
        public void AttachDbInfo(Player player)
        {
            string sql =
                "SELECT id, player_visits FROM prefix_players " +
                "WHERE player_keyhash = @keyhash";

            using (DbDataReader reader = Query(sql, "@keyhash", player.KeyHash))
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    player.DatabaseId = RI(reader, "id");
                    player.TotalVisits = RI(reader, "player_visits");
                }
                else Logger.Log(LogLevel.Warning, "Couldn't find player info for player \"{0}\". (keyhash: {1})", player.Name, player.KeyHash);
            }
            player.IsBanned = IsBanned(player);
        }

        public GameInfo GetLastOpenGame()
        {
            string sql = "SELECT * FROM prefix_stats_games WHERE " +
                "id = (SELECT MAX(id) FROM prefix_stats_games) AND " +
                "game_ended_timestamp = 0";

            using (DbDataReader reader = Query(sql))
            {
                if (reader.Read())
                {
                    return new GameInfo(RL(reader, "id"),
                        GetDateTime(RU(reader, "game_started_timestamp")),
                        RS(reader, "game_map"),
                        RS(reader, "game_mode"),
                        RI(reader, "game_team1_score"),
                        RI(reader, "game_team2_score"),
                        RI(reader, "game_team1_tickets"),
                        RI(reader, "game_team2_tickets"));
                }
            }
            return null;
        }

        public void CloseGame(GameInfo game)
        {
            //stroring winning team in game_team_won se we can query won/lost games fast later on

            string sql = "UPDATE prefix_stats_games SET " +
                "game_ended_timestamp = @timestamp, " +
                "game_team1_score = @score1, " +
                "game_team2_score = @score2, " +
                "game_team1_tickets = @tickets1, " +
                "game_team2_tickets = @tickets2," +
                "game_team_won = @team_won " +
                "WHERE id = @id";

            NonQuery(sql,
                "@id", game.DatabaseId,
                "@timestamp", GetTimestamp(),
                "@score1", game.Team1Score,
                "@score2", game.Team2Score,
                "@tickets1", game.Team1Tickets,
                "@tickets2", game.Team2Tickets,
                "@team_won", game.WinningTeam);
        }

        public void InsertGame(GameInfo game)
        {
            string sql = "INSERT INTO prefix_stats_games " +
                "(game_map, game_mode, game_started_timestamp, game_ended_timestamp, game_team1_score, game_team2_score, game_team1_tickets, game_team2_tickets) VALUES " +
                "(@map, @mode, @started, 0, 0, 0, 0, 0)";
            NonQuery(sql, "@map", game.Map, "@mode", game.Mode.ToString(), "@started", GetTimestamp());
        }

        public void InsertPlayerStats(Player player, GameInfo game, bool quit = false)
        {
            string sql = "INSERT INTO prefix_stats " +
                "(player_id, stat_kills, stat_deaths, stat_points, stat_team, stat_quit, game_id) VALUES " +
                "(@player_id, @kills, @deaths, @points, @team, @quit, @game_id)";

            NonQuery(sql,
                "@player_id", player.DatabaseId,
                "@kills", player.Kills,
                "@deaths", player.Deaths,
                "@points", player.Score,
                "@team", player.Team,
                "@quit", (quit ? 1 : 0),
                "@game_id", game.DatabaseId);
        }

        public PlayerStatistics GetPlayerStats(Player player)
        {
            string sql =
                "SELECT " +
                    "coalesce(sum(stat_kills), 0) as total_kills, " +
                    "coalesce(sum(stat_deaths), 0) as total_deaths, " +
                    "coalesce(sum(stat_points), 0) as total_score, " +
                    "(SELECT count(*) FROM prefix_stats_games INNER JOIN prefix_stats ON game_id = prefix_stats_games.id LEFT JOIN prefix_teams ON team_name = stat_team WHERE player_id = @player_id and game_team_won = team_number) as games_won, " +
                    "(SELECT count(*) FROM prefix_stats_games INNER JOIN prefix_stats ON game_id = prefix_stats_games.id LEFT JOIN prefix_teams ON team_name = stat_team WHERE player_id = @player_id and game_team_won != team_number) as games_lost, " +
                    "(SELECT count(*) FROM prefix_stats_games INNER JOIN prefix_stats ON game_id = prefix_stats_games.id AND stat_quit = 1 WHERE player_id = @player_id) as games_quit " +
                "FROM prefix_stats " +
                    "WHERE player_id = @player_id";

            using (DbDataReader reader = Query(sql, "@player_id", player.DatabaseId))
            {
                if (reader.Read())
                {
                    return new PlayerStatistics(
                        RI(reader, "games_won"),
                        RI(reader, "games_lost"),
                        RI(reader, "games_quit"),
                        RI(reader, "total_kills"),
                        RI(reader, "total_deaths"),
                        RI(reader, "total_score"));
                }
            }
            return null;
        }

        public GameInfo ReadMatch(DbDataReader reader)
        {
            return new GameInfo(
                            RL(reader, "id"),
                            GetDateTime(RU(reader, "game_ended_timestamp")),
                            RS(reader, "game_map"),
                            RS(reader, "game_mode"),
                            RI(reader, "game_team1_score"),
                            RI(reader, "game_team2_score"),
                            RI(reader, "game_team1_tickets"),
                            RI(reader, "game_team2_tickets"),
                            RI(reader, "game_selected") == 1,
                            GetDateTime(RU(reader, "game_started_timestamp")),
                            RS(reader, "game_name"));
        }

        public GameInfo GetMatch(int id)
        {
            string sql = "SELECT * FROM prefix_stats_games WHERE id = @game_id";
            using (DbDataReader reader = Query(sql, "@game_id", id))
            {
                if (reader.Read()) return ReadMatch(reader);
                else return null;
            }
        }

        public List<GameInfo> GetMatches(string nameExp, string mapExp, bool onlySelected, DateTime dateFrom, DateTime dateUntil, int page, int maxRows)
        {
            string where = string.Empty;


            if (onlySelected) where += "game_selected = 1 AND ";
            where += "game_started_timestamp > @from_timestamp AND game_started_timestamp < @until_timestamp";

            if (nameExp.Length > 0)
            {
                nameExp = $"%{nameExp}%";
                where += " AND game_name like @name_exp";
            }

            if (mapExp.Length > 0)
            {
                mapExp = $"%{mapExp}%";
                where += " AND game_map like @map_exp";
            }

            where += " AND game_ended_timestamp > 0";

            string sql = $"SELECT * FROM prefix_stats_games WHERE {where} ORDER BY game_started_timestamp DESC LIMIT @page,@max_rows";

            List<GameInfo> stats = new List<GameInfo>();
            using (DbDataReader reader = Query(sql,
                "@name_exp", nameExp,
                "@map_exp", mapExp,
                "@from_timestamp", GetTimestamp(dateFrom),
                "@until_timestamp", GetTimestamp(dateUntil),
                "@page", page * maxRows,
                "@max_rows", maxRows))
            {
                while (reader.Read())
                {
                    stats.Add(ReadMatch(reader));
                }
            }

            return stats;
        }
        public List<Player> GetMatchPlayerStats(int gameID)
        {
            string sql = "SELECT " +
                "prefix_stats.id, stat_kills, stat_deaths, stat_points, stat_team, player_last_name, player_keyhash " +
                "FROM prefix_stats " +
                "INNER JOIN prefix_players ON player_id = prefix_players.id " +
                "WHERE game_id = @game_id";

            List<Player> stats = new List<Player>();
            using (DbDataReader reader = Query(sql, "@game_id", gameID))
            {
                while (reader.Read())
                {
                    stats.Add(new Player(
                        RI(reader, "id"),
                        RI(reader, "stat_kills"),
                        RI(reader, "stat_deaths"),
                        RI(reader, "stat_points"),
                        RS(reader, "player_last_name"),
                        RS(reader, "player_keyhash"),
                        RS(reader, "stat_team")));
                }
            }
            return stats;
        }

        public void DeleteMatch(int id)
        {
            NonQuery("DELETE FROM prefix_stats WHERE game_id = @game_id", "@game_id", id);
            NonQuery("DELETE FROM prefix_stats_games WHERE id = @game_id", "@game_id", id);
        }

        public void UpdateMatchSelected(int id, bool selected)
        {
            NonQuery("UPDATE prefix_stats_games SET game_selected = @selected WHERE id = @game_id", "@selected", selected ? 1 : 0, "@game_id", id);
        }

        public void UpdateMatchName(int id, string name)
        {
            NonQuery("UPDATE prefix_stats_games SET game_name = @name WHERE id = @game_id", "@name", name, "@game_id", id);
        }

        public int GetTotalMatches()
        {
            DbDataReader reader = Query("SELECT count(*) FROM prefix_stats_games as match_count");
            return RI(reader, "match_count");

        }

        private int GetStatSum(string field)
        {
            DbDataReader reader = Query($"SELECT sum({field}) FROM prefix_stats as total_sum");
            return RI(reader, "total_sum");
        }

        public int GetTotalKills()
        {
            return GetStatSum("stat_kills");
        }

        public int GetTotalPoints()
        {
            return GetStatSum("stat_points");
        }

        public int GetTotalDeaths()
        {
            return GetStatSum("stat_deaths");
        }

        public int GetTotalPlayers()
        {
            DbDataReader reader = Query("SELECT count(*) FROM prefix_players as player_count");
            return RI(reader, "player_count");
        }

        #endregion

        #region Web
        public WebUser GetWebUser(string username, string password)
        {
            string sql =
                "SELECT " +
                "id, user_name, user_password, user_lastvisit " +
                "FROM " +
                "prefix_web_users " +
                "WHERE user_name = @username";

            using (DbDataReader reader = Query(sql, "@username", username))
            {
                if (reader.Read())
                {
                    var hash = RS(reader, "user_password");

                    //update legacy hash
                    if (hash.Length == 32)
                    {
                        hash = PBKDF2.HashPassword(hash);
                        UpdateWebUser(
                            new WebUser(RL(reader, "id"), RS(reader, "user_name"), hash, GetDateTime(RU(reader, "user_lastvisit"))),
                            true);
                    }

                    if (PBKDF2.VerifyPassword(password, hash))
                    {
                        return new WebUser(RL(reader, "id"), RS(reader, "user_name"), hash, GetDateTime(RU(reader, "user_lastvisit")));
                    }
                }
            }

            return null;
        }
        public void UpdateLastSeen(WebUser user)
        {
            string sql = "UPDATE prefix_web_users SET user_lastvisit = @timestamp WHERE id = @user_id";

            NonQuery(sql, "@timestamp", GetTimestamp(), "@user_id", user.Id);
        }
        public List<WebUser> GetWebUsers()
        {
            List<WebUser> users = new List<WebUser>();
            string sql = "SELECT * FROM prefix_web_users";

            using (DbDataReader reader = Query(sql))
            {
                while (reader.Read())
                {
                    users.Add(new WebUser(RL(reader, "id"), RS(reader, "user_name"), RS(reader, "user_password"), GetDateTime(RU(reader, "user_lastvisit"))));
                }
            }
            return users;
        }

        public void InsertWebUser(WebUser user)
        {
            string sql = "INSERT INTO prefix_web_users " +
                "(user_name, user_password, user_lastvisit) VALUES " +
                "(@username, @password, 0)";
            NonQuery(sql, "@username", user.Username, "@password", user.PasswordHash);
        }
        public void UpdateWebUser(WebUser user, bool updatePwd)
        {
            string sql = "UPDATE prefix_web_users SET " +
                "user_name = @username ";
            if (updatePwd) sql += ",user_password = @password ";
            sql += "WHERE id = @user_id";

            NonQuery(sql, "@username", user.Username, "@password", user.PasswordHash, "@user_id", user.Id);
        }
        public void DeleteWebUser(WebUser user)
        {
            string sql = "DELETE FROM prefix_web_users WHERE id = @user_id";
            NonQuery(sql, "@user_id", user.Id);
        }

        public bool WebUserExists()
        {
            using (DbDataReader reader = Query("SELECT id from prefix_web_users"))
            {
                return HasRows(reader);
            }
        }

        public void TruncateWebUsers()
        {
            NonQuery("DELETE FROM prefix_web_users");
        }

        #endregion

        #region "Maps"
        public void ImportMaps(List<ServerMap> maps, bool truncate = true)
        {
            if (truncate) NonQuery("DELETE FROM prefix_maps");

            List<string> sqlParams = new List<string>();

            string sql = "INSERT INTO prefix_maps (map_name, map_nice_name, map_gametype_flags) VALUES ";
            int i = 0;

            foreach (ServerMap mp in maps)
            {
                sql += string.Format("(@map_name_{0},@map_nice_name_{0},@map_flags_{0}),", i.ToString());
                sqlParams.Add("@map_name_" + i.ToString());
                sqlParams.Add(mp.Name);
                sqlParams.Add("@map_nice_name_" + i.ToString());
                sqlParams.Add(mp.NiceName);
                sqlParams.Add("@map_flags_" + i.ToString());
                sqlParams.Add(mp.Flags.ToString());
                i++;
            }

            if (i > 0)
            {
                Logger.Log(LogLevel.Info, "Importing {0} maps", i.ToString());
                sql = sql.Substring(0, sql.Length - 1);
                NonQuery(sql, sqlParams.ToArray());
            }
        }
        public List<ServerMap> GetMaps(string exp = "", string niceExp = "")
        {
            List<ServerMap> maps = new List<ServerMap>();

            string sql =
                "SELECT " +
                "* " +
                "FROM " +
                "prefix_maps";

            if (exp != "")
            {
                exp = $"%{exp}%";
                sql += " WHERE map_name like @exp";
            }

            if (niceExp != "")
            {
                niceExp = $"%{niceExp}%";
                sql += " OR map_nice_name LIKE @nice_exp";
            }


            using (DbDataReader reader = Query(sql, "@exp", exp, "@nice_exp", niceExp))
            {
                while (reader.Read())
                {
                    maps.Add(new ServerMap(RL(reader, "id"), RS(reader, "map_name"), RS(reader, "map_nice_name"), RL(reader, "map_gametype_flags")));
                }
            }

            return maps;
        }

        #endregion

    }
}