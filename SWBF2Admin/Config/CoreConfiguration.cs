using System.Net;
using System.Xml.Serialization;

using SWBF2Admin.Database;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Config
{
    [ConfigFileInfo(fileName: "./cfg/core.xml", template: "SWBF2Admin.Resources.cfg.core.xml")]
    public class CoreConfiguration
    {
        #region "webAdmin"
        public bool WebAdminEnable { get; set; } = true;
        public string WebAdminPrefix { get; set; } = "http://localhost:8080/";
        #endregion

        #region "gameserver"
        public bool ManageServer { get; set; } = true;
        public bool AutoLaunchServer { get; set; } = false;
        public bool AutoRestartServer { get; set; } = false;
        public string ServerPath { get; set; } = "./server";
        public string ServerArgs { get; set; } = "/win /norender /nosound /autonet dedicated /resolution 640 480";

        private bool enableRuntime = true;
        public bool EnableRuntime
        {
            get { return (enableRuntime && !EnableSteamMode); }
            set { enableRuntime = value; }
        }
        public bool EnableSteamMode { get; set; } = false;
        #endregion

        #region "rcon"
        public string RconHostname { get; set; } = "192.168.188.123:4658";
        public string RconPassword { get; set; } = "specialsauce";

        [XmlIgnore]
        public virtual IPEndPoint GetRconIPEP
        {
            get
            {
                string[] cc = RconHostname.Split(':');
                return new IPEndPoint(IPAddress.Parse(cc[0]), (cc.Length > 1 ? int.Parse(cc[1]) : 4658));
            }
        }
        #endregion

        #region "SQL"
        public DbType SQLType { get; set; } = DbType.SQLite;
        public string SQLiteFileName { get; set; } = "./SWBF2Admin.sqlite";
        public string MySQLDatabaseName { get; set; } = "swbf2";
        public string MySQLHostname { get; set; } = "localhost:3306";
        public string MySQLUsername { get; set; } = "root";
        public string MySQLPassword { get; set; } = "";
        #endregion

        #region "commands"
        public string CommandPrefix { get; set; } = "!";
        public bool CommandEnableDynamic { get; set; } = true;
        #endregion

        #region "logger"
        public LogLevel LogMinimumLevel { get; set; } = LogLevel.VerboseSQL;
        public bool LogToFile { get; set; } = false;
        public string LogFileName { get; set; } = "./log.txt";
        #endregion

        #region "misc"
        public int TickDelay { get; set; } = 10;
        #endregion
    }
}