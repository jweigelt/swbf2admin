/* 
 * This file is part of kf2 adminhelper.
 * 
 * SWBF2 SADS-Administation Helper is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * kf2 adminhelper is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with kf2 adminhelper.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Data;
using System.Data.SQLite;
using System.Data.Common;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Structures;
using SWBF2Admin.Web;

namespace SWBF2Admin.Database
{
    public enum DatabaseEngine
    {
        SQLite,
        MySQL
    }
    class SQLHandler
    {
        public DatabaseEngine SQLEngine { get; set; } = DatabaseEngine.SQLite;

        public string SQLTablePrefix { get; set; } = "swbf_";
        public string SQLiteFileName { get; set; } = "SWBF2Admin.sqlite";
        public string MySQLHost { get; set; } = "localhost:3306";
        public string MySQLUser { get; set; } = "root";
        public string MySQLPassword { get; set; } = "";
        public string MySQLDatabase { get; set; } = "swbf2";

        private SQLiteConnection connection = null;

        public void Open()
        {
            if (connection != null)
            {
                Logger.Log(LogLevel.Warning, Log.SQL_ALREADY_OPEN);
                return;
            }

            Logger.Log(LogLevel.Verbose, Log.SQL_OPEN);
            connection = new SQLiteConnection(string.Format("Data Source={0};", SQLiteFileName));

            try
            {
                connection.Open();
                Logger.Log(LogLevel.Info, Log.SQL_OPEN_OK);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, Log.SQL_OPEN_ERROR, e.Message);
                throw e;
            }
        }
        public void Close()
        {
            if (connection != null)
            {
                Logger.Log(LogLevel.Info, Log.SQL_CLOSE);
                connection.Close();
                connection = null;
            }
        }

        DbDataReader Query(string query, params string[] parameters)
        {
            Logger.Log(LogLevel.VerboseSQL, Log.SQL_QUERY, query);
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

                Logger.Log(LogLevel.Error, Log.SQL_QUERY_ERRPR, e.Message);
                return null;
            }
        }
        void NonQuery(string query, params string[] parameters)
        {
            Logger.Log(LogLevel.VerboseSQL, Log.SQL_NONQUERY, query);
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                BuildCommand(query, parameters).ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, Log.SQL_NONQUERY_ERROR, e.Message);
            }
        }
        private SQLiteCommand BuildCommand(string query, params string[] parameters)
        {
            query = query.Replace("prefix_", SQLTablePrefix);

            SQLiteCommand command = new SQLiteCommand(query, connection);

            for (int i = 0; i < parameters.Length; i += 2)
            {
                if (parameters.Length < i)
                {
                    Logger.Log(LogLevel.Error, Log.SQL_PARAMETER_NOVALUE, parameters[i]);
                }
                else
                {
                    command.Parameters.AddWithValue(parameters[i], parameters[i + 1]);
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
        public bool HasPermission(Player player, string permission)
        {
            string sql =
                "SELECT " +
                "prefix_players.id " +
                "FROM " +
                "prefix_permissions ," +
                "prefix_players " +
                "INNER JOIN prefix_players_groups ON prefix_permissions.group_id = prefix_players_groups.group_id AND prefix_players_groups.player_id = prefix_players.id " +
                "WHERE prefix_players.keyhash = @keyhash AND prefix_permissions.permission_name = @permission";

            return (HasRows(Query(sql, "@steam_id", player.KeyHash, "@permission", permission)));
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

        #region Old
        /*
     public void InsertPlayer(Player player)
     {
         string sql =
             "INSERT INTO " +
             "prefix_players " +
             "(player_name, player_keyhash, player_last_ip, player_last_visit, player_first_visit, player_visits) VALUES " +
             "(@name, @keyhash, @last_ip, @last_visit, @first_visit, 1); " +
             "SELECT last_insert_rowid();";

         DbDataReader reader = Query(sql, "@name", player.PlayerName, "@steam_id", player.SteamId, "@unique_net_id", player.UqNetId, "@ip_address", player.IpAddress.ToString(), "@timestamp", GetTimestamp().ToString());
         if (reader != null)
         {
             reader.Read();
             player.DatabaseId = (long)reader[0];
             reader.Close();
         }
     }
     public void UpdatePlayer(Player player)
     {
         string sql =
             "UPDATE kf2_players SET " +
             "player_name = @name, " +
             "player_ip_address = @ip_address, " +
             "player_last_visit = @timestamp, " +
              "player_visits = (player_visits + 1) " +
             "WHERE player_steam_id = @steam_id";

         NonQuery(sql, "@name", player.PlayerName, "@ip_address", player.IpAddress.ToString(), "@timestamp", GetTimestamp().ToString(), "@steam_id", player.SteamId);
     }

     public List<PlayerGroup> GetPlayerGroups(Player player)
     {
         List<PlayerGroup> groups = new List<PlayerGroup>();
         string sql =
             "SELECT " +
             "kf2_groups.group_name, " +
             "kf2_groups.id " +
             "FROM " +
             "kf2_players " +
             "INNER JOIN kf2_player_groups ON kf2_player_groups.player_id = kf2_players.id " +
             "INNER JOIN kf2_groups ON kf2_player_groups.group_id = kf2_groups.id " +
             "WHERE kf2_players.player_steam_id = @steam_id";

         DbDataReader reader = Query(sql, "@steam_id", player.SteamId);
         if (reader != null)
         {
             while (reader.Read())
             {
                 groups.Add(new PlayerGroup((long)reader["id"], (string)reader["group_name"]));
             }
             reader.Close();
         }

         return groups;
     }


     public List<PlayerGroup> GetAllPlayerGroups()
     {
         List<PlayerGroup> groups = new List<PlayerGroup>();
         string sql =
             "SELECT " +
             "id, " +
             "group_name " +
             "FROM " +
             "kf2_groups";

         DbDataReader reader = Query(sql);
         if (reader != null)
         {
             while (reader.Read())
             {
                 groups.Add(new PlayerGroup((long)reader["id"], (string)reader["group_name"]));
             }
             reader.Close();
         }

         return groups;
     }

     public long GetPlayerId(Player player)
     {
         long playerId = -1;
         string sql =
            "SELECT " +
            "id " +
            "FROM " +
            "kf2_players " +
            "WHERE kf2_players.player_steam_id = @steam_id";

         DbDataReader reader = Query(sql, "@steam_id", player.SteamId);
         if (reader != null)
         {
             reader.Read();
             playerId = (long)reader["id"];
             reader.Close();
         }
         return playerId;
     }

     public void SetPlayerGroup(Player player, PlayerGroup group, bool delete = false)
     {
         string sql = null;
         long playerId = GetPlayerId(player);

         if (delete)
         {
             sql =
                 "DELETE FROM kf2_player_groups " +
                 "WHERE group_id = @group_id AND player_id = @player_id";
         }
         else
         {
             sql =
                 "INSERT INTO kf2_player_groups " +
                 "(group_id, player_id) VALUES " +
                 "(@group_id, @player_id)";
         }


         NonQuery(sql, "@group_id", group.Id.ToString(), "@player_id", playerId.ToString());
     }

     public void TrackStats(Player player)
     {
         string sql =
           "UPDATE kf2_players SET " +
           "player_kills = (player_kills + @kills) " +
           "WHERE player_steam_id = @steam_id";

         NonQuery(sql, "@kills", player.Kills.ToString(), "@steam_id", player.SteamId);
     }

     public bool GroupListEmpty()
     {
         string sql =
             "SELECT " +
             "id " +
             "FROM " +
             "kf2_player_groups";

         return !HasRows(Query(sql));
     }


     */
        #endregion


    }
}