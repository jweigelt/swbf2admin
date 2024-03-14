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
using System.Net;
using System.Xml.Serialization;

using SWBF2Admin.Database;
using SWBF2Admin.Utility;
namespace SWBF2Admin.Config
{
    public enum GameserverType
    {
        GoG,
        Steam,
        Gamespy,
        Aspyr
    }

    [ConfigFileInfo(fileName: "./cfg/core.xml", template: "SWBF2Admin.Resources.cfg.core.xml")]
    public class CoreConfiguration
    {
        #region Web Admin
        public bool WebAdminEnable { get; set; } = true;
        public string WebAdminPrefix { get; set; } = "http://localhost:8080/";
        #endregion

        #region Gameserver
        public bool AutoLaunchServer { get; set; } = false;
        public bool AutoRestartServer { get; set; } = true;
        public string ServerPath { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Battle";
        public string ServerArgs { get; set; } = "/win /norender /nosound /nointro /autonet dedicated /resolution 640 480";
        
        public string SteamPath { get; set; } = @"C:\Program Files (x86)\Steam";

        public bool EnableHighPriority { get; set; } = true;
        public bool EnableRuntime { get; set; } = false;
        public GameserverType ServerType { get; set; } = GameserverType.Aspyr;

        public bool EnableEmptyRestart { get; set; } = true;
        public int EmptyRestartThreshold { get; set; } = 3600;
        public int EmptyRestartCheckInterval { get; set; } = 30000;
        #endregion

        #region SQL
        public DbType SQLType { get; set; } = DbType.SQLite;
        public string SQLiteFileName { get; set; } = "./SWBF2Admin.sqlite";
        public string MySQLDatabaseName { get; set; } = "swbf2";
        public string MySQLHostname { get; set; } = "localhost:3306";
        public string MySQLUsername { get; set; } = "root";
        public string MySQLPassword { get; set; } = "";
        #endregion

        #region Commands
        public string CommandPrefix { get; set; } = "!";
        public bool CommandEnableDynamic { get; set; } = true;
        #endregion

        #region Logger
        public LogLevel LogMinimumLevel { get; set; } = LogLevel.VerboseSQL;
        public bool LogToFile { get; set; } = false;
        public string LogFileName { get; set; } = "./log.txt";
        #endregion

        #region Misc
        public int TickDelay { get; set; } = 10;
        public int RuntimeStartDelay { get; set; } = 500;
        #endregion
    }
}