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
    class SQLHandler : ComponentBase
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
        DbDataReader Query(string query, params string[] parameters)
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
        void NonQuery(string query, params string[] parameters)
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
        private DbCommand BuildCommand(string query, params string[] parameters)
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
                    Logger.Log(LogLevel.Error, "[SQL] No value for parameter '{0}' specified", parameters[i]);
                }
                else
                {
                    DbParameter p = command.CreateParameter();
                    p.ParameterName = parameters[i];
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
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        private uint GetTimestamp(DateTime date)
        {
            return (uint)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        private DateTime GetDateTime(uint timestamp)
        {
            DateTime r = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
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
        private long RL(DbDataReader reader, string field)
        {
            return (long)reader[field];
        }
        #endregion
        #region Ingame
        public bool PlayerExists(Player player)
        {
            string sql =
                "SELECT " +
                "id " +
                "FROM " +
                "prefix_payers " +
                "WHERE keyhash = @keyhash";

            return (HasRows(Query(sql, "@keyhash", player.KeyHash)));
        }
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
        #region Web
        public WebUser GetWebUser(string username, string password)
        {
            string sql =
                "SELECT " +
                "id, user_name, user_lastvisit " +
                "FROM " +
                "prefix_web_users " +
                "WHERE user_name = @username AND user_password = @password";

            using (DbDataReader reader = Query(sql, "@username", username, "@password", Util.Md5(password)))
            {
                if (HasRows(reader))
                {
                    reader.Read();
                    return new WebUser(RL(reader, "id"), RS(reader, "user_name"), GetDateTime(RU(reader, "user_lastvisit")));
                }
            }

            return null;
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
                "ban_reason LIKE @reason_exp" +
                " AND (ban_timestamp) > @date_timestamp";

            if (!expired) sql += " AND (ban_timestamp + ban_duration) > @timestamp";
            if (banType != (int)BanType.ShowAll) sql += " AND ban_type = @ban_type";

            sql += " ORDER BY ban_timestamp LIMIT @max_rows";


            List<PlayerBan> bans = new List<PlayerBan>();
            using (DbDataReader reader = Query(sql,
                "@player_exp", playerExp,
                "@admin_exp", adminExp,
                "@reason_exp", reasonExp,
                "@timestamp", GetTimestamp().ToString(),
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
                        (BanType)(RU(reader, "ban_type"))));
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
        public List<ServerMap> GetMaps()
        {
            List<ServerMap> maps = new List<ServerMap>();

            string sql =
                "SELECT " +
                "* " +
                "FROM " +
                "prefix_maps";

            using (DbDataReader reader = Query(sql))
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