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
using System.Threading;
using System.IO;

namespace SWBF2Admin.Utility
{
    public enum LogLevel
    {
        VerboseSQL = 0,
        Verbose = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
    }

    static class Logger
    {
        private static Mutex mtx = new Mutex();

        public static LogLevel MinLevel { get; set; } = LogLevel.Verbose;
        public static bool LogToFile { get; set; } = false;
        public static string LogFile { get; set; } = "/log.txt";

        public static void Log(LogLevel logLevel, string message, params string[] args)
        {

            string status = string.Empty;
            string time = string.Empty;

            if (logLevel < MinLevel) return;
            message = string.Format(message, args);
            message = "| " + message;

            time = "[" + DateTime.Now.ToString() + "] ";

            if (mtx.WaitOne(Constants.MUTEX_LOCK_TIMEOUT))
            {
                Console.Write(time);
                switch (logLevel)
                {
                    case LogLevel.Verbose:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        status = "DEBUG ";
                        break;
                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        status = "INFO  ";
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        status = "WARN  ";
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        status = "ERROR ";
                        break;
                }

                Console.Write(status);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(message);

                if (LogToFile)
                {
                    File.AppendAllText(LogFile, time + status + message + "\r\n");

                }
                mtx.ReleaseMutex();
            }
        }

        public static void Info(string message, params string[] args)
        {
            Logger.Log(LogLevel.Info, message, args);
        }

        public static void Warn(string message, params string[] args)
        {
            Logger.Log(LogLevel.Warning, message, args);
        }

        public static void Error(string message, params string[] args)
        {
            Logger.Log(LogLevel.Error, message, args);
        }
    }
}