using System.Net;
using System.Xml.Serialization;

using SWBF2Admin.Database;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Config
{
    public class CoreConfiguration
    {
        [XmlIgnore]
        public const string FILE_NAME = "./cfg/core.xml";

        [XmlIgnore]
        public const string RESOURCE_NAME = "SWBF2Admin.Resources.cfg.core.xml";

        /** 
         *  Web Admin  
         **/
        public bool WebAdminEnable { get; set; } = true;
        public string WebAdminPrefix { get; set; } = "http://localhost:8080/";

        public bool ManageServer { get; set; } = true;
        public bool AutoLaunchServer { get; set; } = false;
        public bool AutoRestartServer { get; set; } = false;
        public string ServerPath { get; set; } = "./server";

        /**
         *   Remote console 
         **/
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

        /**
         *  Database backend
         **/
        public DbType SQLType { get; set; } = DbType.SQLite;
        public string SQLiteFileName { get; set; } = "./SWBF2Admin.sqlite";
        public string MySQLDatabaseName { get; set; } = "swbf2";
        public string MySQLHostname { get; set; } = "localhost:3306";
        public string MySQLUsername { get; set; } = "root";
        public string MySQLPassword { get; set; } = "";

        /**
         *  CommandHandler
         **/
        public string CommandPrefix { get; set; } = "!";

        /**
         *  Logging 
         **/
        public LogLevel LogMinimumLevel { get; set; } = LogLevel.VerboseSQL;
        public bool LogToFile { get; set; } = false;
        public string LogFileName { get; set; } = "./log.txt";

        public int TickDelay { get; set; } = 10;

    }
}