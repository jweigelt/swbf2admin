
using System;
using System.Threading;
using System.IO;

namespace IngameControllerServer
{
    public enum LogLevel
    {
        VerboseSQL = 0,
        Verbose = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
    }

    public static class Logger
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

            if (mtx.WaitOne(10))
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