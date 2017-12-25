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
using SWBF2Admin.Runtime.Players;

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

        #region Util
        private bool HasRows(DbDataReader reader)
        {
            if (reader == null) return false;
            return reader.HasRows;
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
            return (uint)(long)reader[field];
        }
        private int RI(DbDataReader reader, string field)
        {
            return (int)(long)reader[field];
        }

        private long RL(DbDataReader reader, string field)
        {
            return (long)reader[field];
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
        public List<PlayerBan> GetBans(string playerExp, string adminExp, string reasonExp, bool expired, int banType, DateTime date, int maxRows)
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
                "prefix_players_admins.player_last_name AS admin_last_name, " +
                "prefix_players.player_keyhash, " +
                "prefix_players.player_last_ip " +
                "FROM " +
                "prefix_bans " +
                "INNER JOIN prefix_players ON prefix_players.id = prefix_bans.player_id " +
                "INNER JOIN prefix_players AS prefix_players_admins ON prefix_players_admins.id = prefix_bans.admin_id " +
                "WHERE " +
                "prefix_players.player_last_name LIKE @player_exp AND " +
                "admin_last_name LIKE @admin_exp AND " +
                "ban_reason LIKE @reason_exp AND " +
                "(ban_timestamp) > @date_timestamp";

            if (!expired) sql += " AND ((ban_timestamp + ban_duration) > @timestamp)";
            if (banType != (int)BanType.ShowAll) sql += " AND ban_type = @ban_type";

            sql += " ORDER BY ban_timestamp LIMIT @max_rows";


            List<PlayerBan> bans = new List<PlayerBan>();
            using (DbDataReader reader = Query(sql,
                "@player_exp", playerExp,
                "@admin_exp", adminExp,
                "@reason_exp", reasonExp,
                "@timestamp", GetTimestamp(),
                "@max_rows", maxRows.ToString(),
                "@date_timestamp", GetTimestamp(date).ToString(),
                "@ban_type", banType.ToString()))
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
        public void DeleteBan(int id)
        {
            string sql =
                "DELETE FROM prefix_bans " +
                "WHERE id = @id";
            NonQuery(sql, "@id", id.ToString());
        }
        #endregion

        #region Player tracking
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
                "SELECT * FROM prefix_players " +
                "WHERE player_keyhash = @keyhash";

            using (DbDataReader reader = Query(sql, "@keyhash", player.KeyHash))
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    player.DatabaseId = RL(reader, "id");
                    player.IsBanned = IsBanned(player);
                }
                else Logger.Log(LogLevel.Warning, "Couldn't find player info for player \"{0}\". (keyhash: {1})", player.Name, player.KeyHash);
            }
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
            string sql = "UPDATE prefix_stats_games SET " +
                "game_ended_timestamp = @timestamp, " +
                "game_team1_score = @score1, " +
                "game_team2_score = @score2, " +
                "game_team1_tickets = @tickets1, " +
                "game_team2_tickets = @tickets2 " +
                "WHERE id = @id";

            NonQuery(sql, "@id", game.DatabaseId,
                "@timestamp", GetTimestamp(),
                "@score1", game.Team1Score,
                "@score2", game.Team2Score,
                "@tickets1", game.Team1Tickets,
                "@tickets2", game.Team2Tickets);
        }

        public void InsertGame(GameInfo game)
        {
            string sql = "INSERT INTO prefix_stats_games " +
                "(game_map, game_started_timestamp, game_ended_timestamp, game_team1_score, game_team2_score, game_team1_tickets, game_team2_tickets) VALUES " +
                "(@map, @started, 0, 0, 0, 0, 0)";
            NonQuery(sql, "@map", game.Map, "@started", GetTimestamp());
        }

        public void AddPlayerStats(Player player, GameInfo game, bool quit = false)
        {
            string sql = "INSERT INTO prefix_stats " +
                "(player_id, stat_kills, stat_deaths, stat_points, stat_team, stat_quit, game_id) VALUES " +
                "(@player_id, @kills, @deaths, @points, @team, @quit, @game_id)";

            NonQuery(sql, "@player_id", player.DatabaseId,
                "@kills", player.Kills,
                "@deaths", player.Deaths,
                "@points", player.Score,
                "@team", player.Team,
                "@quit", (quit ? "1" : "0"),
                "@game_id", game.DatabaseId);
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
                "WHERE user_name = @username AND user_password = @password";

            using (DbDataReader reader = Query(sql, "@username", username, "@password", Util.Md5(password)))
            {
                if (HasRows(reader))
                {
                    reader.Read();
                    return new WebUser(RL(reader, "id"), RS(reader, "user_name"), RS(reader, "user_password"), GetDateTime(RU(reader, "user_lastvisit")));
                }
            }

            return null;
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
                sql += " OR map_name LIKE @nice_exp";
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