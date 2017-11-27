using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using SWBF2Admin.Utility;
using SWBF2Admin.Rcon.Packets;

namespace SWBF2Admin.Rcon
{
    class RconClient
    {
        public event EventHandler RconDisconnected;
        public event EventHandler RconChat;

        public IPEndPoint ServerIPEP { get; set; }
        public string ServerPassword { get; set; }

        private bool running = false;
        private Thread workThread;
        private TcpClient client;

        private BinaryReader reader;
        private BinaryWriter writer;

        private string lastMessage = null;

        public void Start()
        {
            if (running) return;

            client = new TcpClient();

            try
            {
                client.Connect(ServerIPEP);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed to connect to rcon server at '{0}' ({1})", ServerIPEP.ToString(), e.ToString());
                throw new RconException(e);
            }

            reader = new BinaryReader(client.GetStream());
            writer = new BinaryWriter(client.GetStream());
            Logger.Log(LogLevel.Info, "Connected to rcon server at '{0}'. Sending login...", ServerIPEP.ToString());

            try
            {
                Login();
                Logger.Log(LogLevel.Info, "Login OK. Rcon ready.");
            }
            catch (RconNotAuthorizedException e)
            {
                Logger.Log(LogLevel.Error, "Server refused login.");
                throw new RconException((Exception)e);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed to send Login ({0})", e.ToString());
                throw new RconException(e);
            }

            running = true;
            workThread = new Thread(WorkThread_Run);
            workThread.Start();
        }
        public void Stop()
        {
            running = false;
            if (workThread != null) workThread.Join();
        }
        public void SendPacket(RconPacket packet)
        {
            try
            {
                Logger.Log(LogLevel.Verbose, "Sending command: '{0}'", packet.Command);
                Send(packet.Command);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "SendPacket failed. ({0})", e.ToString());
                return;
            }

            lastMessage = null;
            while (lastMessage == null) Thread.Sleep(Constants.RCON_SLEEP);
            packet.HandleResponse(lastMessage);
            lastMessage = null;
        }

        public void Say(string message)
        {
            Send("/say " + message);
        }

        private void Login()
        {
            byte[] buf = Util.StrToBytes(Util.Md5(ServerPassword));
            writer.Write(buf);
            writer.Write(Constants.RCON_LOGIN_MAGIC);
            writer.Flush();
            if (reader.ReadByte() != 0x01) throw new RconNotAuthorizedException();
        }
        private void Send(string command)
        {
            byte[] buffer = Util.StrToBytes(command);

            writer.Write((byte)1);
            writer.Write((byte)(buffer.Length + 1));
            writer.Write(buffer);
            writer.Write((byte)0);
            writer.Flush();
        }

        private void WorkThread_Run()
        {
            string message = string.Empty;
            byte[] buffer = new byte[byte.MaxValue];
            byte rows = 0, rowLen = 0, bytesRead = 0;
            bool eos = false;
            try
            {
                while (running && !eos)
                {
                    rows = reader.ReadByte();
                    for (byte i = 0; i < rows; i++)
                    {
                        rowLen = reader.ReadByte();
                        while (bytesRead < rowLen) bytesRead += (byte)reader.Read(buffer, bytesRead, (rowLen - bytesRead));
                        message += Util.BytesToStr(buffer, rowLen);
                    }

                    if (message.Length > 0)
                    {
                        //Removing zero-termination
                        message = message.Substring(0, message.Length - 1);

                        ProcessMessage(message);
                    }
                    bytesRead = 0;
                    message = string.Empty;
                }

            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Rcon disconnected. {0}", e.ToString());
            }
            if (RconDisconnected != null) RconDisconnected.Invoke(this, new EventArgs());
            reader.Close();
            writer.Close();
            client.Close();
        }
        private void ProcessMessage(string message)
        {
            if (message.Length == 0) return;
            if (HandleChat(message)) return;
            if (HandleStatusMessage(message)) return;

            lastMessage = message;
            while (lastMessage != null) Thread.Sleep(1);
        }
        private bool HandleChat(string message)
        {
            if (message[0] != '\t') return false;

            string[] cc = message.Split('\t');
            if (cc.Length < 2)
            {
                Logger.Log(LogLevel.Warning, "Received invalid chat fragment. ({0})", message);
            }
            else
            {
                Logger.Log(LogLevel.Info, "#{0}:{1}", cc[1], cc[2]);
                if (RconChat != null) RconChat.Invoke(this, new RconChatEventArgs(cc[1], cc[2]));
            }
            return true;
        }
        private bool HandleStatusMessage(string message)
        {
            switch (message)
            {
                case Constants.RCON_STATUS_MESSAGE_GAME_HAS_ENDED:
                    return true;
                default:
                    return false;
            }
        }
    }
}