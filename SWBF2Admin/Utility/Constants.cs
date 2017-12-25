namespace SWBF2Admin
{
    class Constants
    {
        #region ProductInfo
        public const string PRODUCT_NAME = "SWBF2Admin";
        public const string PRODUCT_VERSION = "4 (C#-version)";
        public const string PRODUCT_AUTHOR = "JW 'LeKeks' :: jan@lekeks.de";
        #endregion

        #region Threading
        public const int MUTEX_LOCK_TIMEOUT = 10;
        #endregion

        #region Rcon
        public const byte RCON_LOGIN_MAGIC = 0x64;
        public const byte RCON_SLEEP = 5;
        public const string RCON_STATUS_MESSAGE_GAME_HAS_ENDED = "Game has ended";
        #endregion

        #region Web
        public const string WEB_URL_ROOT = "/";
        public const string WEB_DIR_ROOT = "./web";

        public const string WEB_URL_RESOURCES = "/res";

        public const string WEB_FILE_FRAME = "frame.htm";

        public const string WEB_URL_DASHBOARD = "/live/dashboard";
        public const string WEB_FILE_DASHBOARD = "dashboard.htm";

        public const string WEB_URL_PLAYERS = "/live/players";
        public const string WEB_FILE_PLAYERS = "players.htm";

        public const string WEB_URL_CHAT = "/live/chat";
        public const string WEB_FILE_CHAT = "chat.htm";

        public const string WEB_URL_ABOUT = "/web/about";
        public const string WEB_FILE_ABOUT = "about.htm";

        public const string WEB_URL_BANS = "/live/bans";
        public const string WEB_FILE_BANS = "bans.htm";

        public const string WEB_URL_SETTINGS_GENERAL = "/settings/general";
        public const string WEB_FILE_SETTINGS_GENERAL = "general.htm";

        public const string WEB_URL_SETTINGS_GAME = "/settings/game";
        public const string WEB_FILE_SETTINGS_GAME = "game.htm";

        public const string WEB_URL_SETTINGS_MAPS = "/settings/maps";
        public const string WEB_FILE_SETTINGS_MAPS = "maps.htm";

        public const string WEB_URL_SETTINGS_GROUP = "/settings/groups";
        public const string WEB_FILE_SETTINGS_GROUP = "groups.htm";


        public const string WEB_TAG_USER_NAME = "{account:username}";
        public const string WEB_TAG_USER_LASTVISIT = "{account:lastvisit}";

        public const string WEB_TAG_PRODUCT_NAME = "{product:name}";
        public const string WEB_TAG_PRODUCT_VERSION = "{product:version}";
        public const string WEB_TAG_PRODUCT_AUTHOR = "{product:author}";

        public const string WEB_ACTION_DEFAULT_STATUS_SET = "status_get";
        public const string WEB_ACTION_DASHBOARD_STATUS_GET = "status_get";
        public const string WEB_ACTION_DASHBOARD_STATUS_SET = "status_set";

        public const string WEB_ACTION_PLAYERS_UPDATE = "players_update";
        public const string WEB_ACTION_BANS_UPDATE = "bans_update";
        public const string WEB_ACTION_BANS_DELETE = "bans_delete";

        public const string WEB_ACTION_CHAT_UPDATE = "chat_update";
        public const string WEB_ACTION_CHAT_SEND = "chat_send";

        public const string WEB_COOKIE_CHAT = "chat_session";
        public const int WEB_COOKIE_TIMEOUT = 3600;
        public const int WEB_SESSION_LIMIT = 500;


        #endregion
    }
}