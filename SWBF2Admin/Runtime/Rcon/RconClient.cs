using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using SWBF2Admin.Utility;
using SWBF2Admin.Runtime.Rcon.Packets;
using SWBF2Admin.Config;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Rcon
{
    /// <summary>
    /// class for connecting to the remote console ("rcon") server
    /// </summary>
    public class RconClient : ComponentBase
    {
        public RconClient(AdminCore core) : base(core) { }
        public override void OnServerStart()
        {
            ServerPassword = Core.Server.Settings.AdminPw;
            Start();
        }
        public override void OnServerStop()
        {
            Stop();
        }
        public override void Configure(CoreConfiguration config)
        {
            ServerPassword = config.RconPassword;
            ServerIPEP = config.GetRconIPEP;
        }
        public override void OnDeInit()
        {
            Stop();
        }

        /// <summary>Called when the rcon-connection is lost</summary>
        public event EventHandler Disconnected;

        /// <summary>Called when new chat is received</summary>
        public event EventHandler ChatInput;

        /// <summary>Called when a match ended</summary>
        public event EventHandler GameEnded;

        /// <summary>Rcon-servers IPEndPoint</summary>
        public IPEndPoint ServerIPEP { get; set; }

        /// <summary>server's admin-password</summary>
        public string ServerPassword { get; set; }

        /// <summary>max. time (in ms) before a packet is dropped if the server doesn't respond</summary>
        public int PacketTimeout { get; set; } = 500;

        private bool running = false;

        internal void Pm(string message, Player player)
        {
            throw new NotImplementedException();
        }

        private Thread workThread;
        private TcpClient client;

        private BinaryReader reader;
        private BinaryWriter writer;

        private string lastMessage = null;

        public string FilterString(string cmd)
        {
            //Prevent command injection
            return cmd.Replace('/', '\\');
        }

        /// <summary>Connects to rcon-server and authenticates</summary>
        public void SendPacket(RconPacket packet)
        {
            try
            {
                Logger.Log(LogLevel.Verbose, "Sending command: '{0}'", packet.Command);
                Send(packet.Command);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "SendPacket failed. ({0})", e.ToString());
                return;
            }

            lastMessage = null;
            DateTime start = DateTime.Now;
            while (lastMessage == null)
            {
                if ((DateTime.Now - start).TotalMilliseconds < PacketTimeout)
                {
                    Thread.Sleep(Constants.RCON_SLEEP);
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "Rcon packet timeout. (running {0})", packet.Command);
                    return;
                }
            }

            packet.HandleResponse(lastMessage);
            lastMessage = null;
        }

        /// <summary>Connects to rcon-server and authenticates</summary>
        public void Say(string message)
        {
            SendCommand("say", message);
        }

        public void SendCommand(string command, bool dontwait, params string[] p)
        {
            if (dontwait)
                Send(command + " " + string.Join(" ", p));
            else
                SendCommand(command, p);
        }

        public void SendCommand(string command, params string[] p)
        {
            RconPacket packet = new RconPacket(command + " " + string.Join(" ", p));
            SendPacket(packet);
        }

        /// <summary>Connects to rcon-server and authenticates</summary>
        private void Start()
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

        /// <summary>Closes the current connection</summary>
        private void Stop()
        {
            running = false;
            if (client != null) client.Dispose();
            if (workThread != null) workThread.Join();
        }

        /// <summary>Sends authentication to server</summary>
        private void Login()
        {
            byte[] buf = Util.StrToBytes(Util.Md5(ServerPassword));
            writer.Write(buf);
            writer.Write(Constants.RCON_LOGIN_MAGIC);
            writer.Flush();
            if (reader.ReadByte() != 0x01) throw new RconNotAuthorizedException();
        }

        /// <summary>Sends a raw string to the server</summary>
        private void Send(string command)
        {
            //filter any /'s to prevent command injection
            byte[] buffer = Util.StrToBytes(FilterString(command));

            writer.Write((byte)1);
            writer.Write((byte)(buffer.Length + 2));
            writer.Write('/');
            writer.Write(buffer);
            writer.Write((byte)0);
            writer.Flush();
        }

        /// <summary>Thread handling inbound data</summary>
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
                        message = message.Substring(0, message.Length - 1);
                        if (i + 1 < rows) message = message + "\n";

                        bytesRead = 0;
                    }

                    ProcessMessage(message);

                    bytesRead = 0;
                    message = string.Empty;
                }

            }
            catch (Exception e)
            {
                if (running) Logger.Log(LogLevel.Warning, "Rcon disconnected. {0}", e.ToString());
            }
            reader.Close();
            writer.Close();
            client.Close();
            running = false;

            InvokeEvent(Disconnected, this, new EventArgs());
        }

        /// <summary>Processes messages received by the server</summary>
        /// <param name="message">messaged received from server</param>
        private void ProcessMessage(string message)
        {
            if (message.Length > 0)
            {
                if (HandleChat(message)) return;
                if (HandleStatusMessage(message)) return;
            }

            lastMessage = message;

            DateTime start = DateTime.Now;
            while (lastMessage != null)
            {
                if ((DateTime.Now - start).TotalMilliseconds < PacketTimeout)
                {
                    Thread.Sleep(Constants.RCON_SLEEP);
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "Rcon response was not processed in time, dropping it.");
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if the specified message is a chat-message. If so, the message is processed.
        /// <para>If the message was processed, true is returned.</para>
        /// </summary>
        /// <param name="message">messaged received from server</param>
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
                InvokeEvent(ChatInput, this, new RconChatEventArgs(cc[1], cc[2]));
            }
            return true;
        }

        /// <summary>
        /// Checks if the specified message is a status-message. If so, the message is processed.
        /// <para>If the message was processed, true is returned.</para>
        /// </summary>
        /// <param name="message">messaged received from server</param>
        private bool HandleStatusMessage(string message)
        {
            switch (message)
            {
                case Constants.RCON_STATUS_MESSAGE_GAME_HAS_ENDED:
                    InvokeEvent(GameEnded, this, null);
                    return true;
                default:
                    return false;
            }
        }
    }
}