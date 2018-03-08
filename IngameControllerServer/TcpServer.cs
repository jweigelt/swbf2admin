using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IngameControllerServer
{
    class TcpServer
    {
        private const byte NET_COMMAND_RDP_OPEN = 0x01;
        private const byte NET_COMMAND_RDP_CLOSE = 0x02;

        private TcpListener listener = null;
        private Thread workThread = null;
        private bool running = false;

        public void Start(IPEndPoint bindIPEP)
        {
            if (running) throw new Exception("Already running.");

            listener = new TcpListener(bindIPEP);
            listener.Start();

            running = true;
            workThread = new Thread(WorkThread_Run);
            workThread.Start();
        }

        public void Stop()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            if (workThread != null && workThread.IsAlive)
            {
                workThread.Join();
                workThread = null;
            }
        }

        private void WorkThread_Run()
        {
            while (running)
            {
                if (listener.Pending())
                {
                    try
                    {
                        using (TcpClient client = listener.AcceptTcpClient())
                        {
                            HandleClient(client);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Warning, "Client exception: {0})", e.Message);
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void StartRDP()
        {
            //StopRDP();
            Logger.Log(LogLevel.Info, "Starting RDP session");
            /*try
            {
                Process.Start(Directory.GetCurrentDirectory() + "/rdp.bat");
            }catch(Exception e)
            {
                Logger.Log(LogLevel.Error, e.Message);
            }*/
          
            ProcessStartInfo info = new ProcessStartInfo("mstsc.exe", "/v:localhost");
            Process.Start(info);
        }

        private void StopRDP()
        {
            //TODO: clean that up
            Logger.Log(LogLevel.Info, "Closing RDP session");

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.MainModule.FileName.EndsWith("mstsc.exe"))
                    {
                        Logger.Log(LogLevel.Info, "Found process with id " + p.Id.ToString() + " -trying to kill it");
                        p.Kill();
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, "Failed to end Process {0} ({1})", p.Id.ToString(), e.Message);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (BinaryReader reader = new BinaryReader(client.GetStream()))
            {
                byte command = reader.ReadByte();
                switch (command)
                {
                    case NET_COMMAND_RDP_OPEN:
                        StartRDP();
                        break;

                    case NET_COMMAND_RDP_CLOSE:
                        StopRDP();
                        break;

                    default:
                        Logger.Log(LogLevel.Warning, "Client at {0} sent unknown command {1}", client.Client.RemoteEndPoint.ToString(), command.ToString());
                        break;
                }
            }
        }

        ~TcpServer()
        {
            Stop();
        }
    }
}
