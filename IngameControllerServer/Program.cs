using System;
using System.Diagnostics;
using System.Net;

namespace IngameControllerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //ProcessStartInfo cmdK = new ProcessStartInfo("cmdkey.exe", "/generic:TERMSRV/inky.lekeks.de /user:gameserver /pass:vbn3tkl4553!");
            //Process.Start(cmdK);
        
            Logger.MinLevel = LogLevel.Verbose;

            TcpServer server = new TcpServer();
            try
            {
                server.Start(new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 1138));
                Logger.Log(LogLevel.Info, "Server is up.");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Couldn't start server: {0}", e.Message);
            }

            Logger.Log(LogLevel.Info, "Press [Return] to exit.");
            Console.ReadLine();
            server.Stop();
        }
    }
}