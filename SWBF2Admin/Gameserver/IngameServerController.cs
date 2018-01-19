using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using System.Diagnostics;

namespace SWBF2Admin.Gameserver
{
    public class IngameServerController : ComponentBase
    {
        private const int OFFSET_MAP_STATUS = (0x01EAFCA0 - 0x00401000 + 0x1000);
        private const int READ_TIMEOUT = 50;
        private const byte NET_COMMAND_RDP_OPEN = 0x01;
        private const byte NET_COMMAND_RDP_CLOSE = 0x02;

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        private bool isLoading = false;
        private bool steamMode;

        private IngameServerControllerConfiguration config;

        private IntPtr moduleBase;
        private IntPtr procHandle = IntPtr.Zero;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        private byte ReadMapStatus()
        {
            return ReadByte(OFFSET_MAP_STATUS);
        }
        private byte ReadByte(int offset)
        {
            IntPtr bytesRead;
            byte[] buf = new byte[1];
            if (!ReadProcessMemory(procHandle, IntPtr.Add(moduleBase, offset), buf, 1, out bytesRead) || bytesRead == IntPtr.Zero)
                throw new Exception("ReadProcessMemory() failed");
            return buf[0];
        }

        private void MemoryInit()
        {
            Process p = Core.Server.ServerProcess;
            if (p != null)
            {
                Logger.Log(LogLevel.Verbose, "Attaching controller to GoG listenserver");
                procHandle = OpenProcess(ProcessAccessFlags.All, false, p.Id);
                if (procHandle == IntPtr.Zero)
                {
                    Logger.Log(LogLevel.Error, "OpenProcess() failed on serverprocess (id: {0})", p.Id.ToString());
                    throw new Exception("OpenProcess() failed.");
                }

                moduleBase = p.MainModule.BaseAddress;
            }
        }
        private void MemoryClose()
        {
            if (procHandle != IntPtr.Zero)
            {
                Logger.Log(LogLevel.Verbose, "Closing handle {0}", procHandle.ToString());
                CloseHandle(procHandle);
            }
        }
 
        private void SendCommand(byte command)
        {
            Logger.Log(LogLevel.Verbose, "Sending controller command: {0}", command.ToString());
            try
            {
                //just using a very primitive single-threaded client which re-connects every time
                //as events requiring net interaction are rather rare, there's no point in keeping a connection alive
                using (TcpClient client = new TcpClient())
                {
                    //use strict timeouts so we don't block the main thread for too long if something goes wrong
                    client.ReceiveTimeout = config.TcpTimeout;
                    client.SendTimeout = config.TcpTimeout;

                    client.Connect(config.ServerIPEP);
                    using (BinaryWriter writer = new BinaryWriter(client.GetStream()))
                    {
                        writer.Write(command);
                        writer.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "SendCommand() failed ({0})", e.Message);
            }

        }

        public override void OnInit()
        {
            Core.Server.SteamServerStarting += Server_SteamServerStarting;
        }
        public IngameServerController(AdminCore core) : base(core)
        {
            UpdateInterval = READ_TIMEOUT;
        }
        public override void Configure(CoreConfiguration config)
        {
            steamMode = config.EnableSteamMode;
            this.config = Core.Files.ReadConfig<IngameServerControllerConfiguration>();

            //TODO: clean that up:
            //calling getter once so any format errors are thrown now (during config) and not during runtime
            IPEndPoint ipep = this.config.ServerIPEP;
        }
        public void Server_SteamServerStarting(object sender, EventArgs e)
        {
            //request RD session for startup
            if (config.Enable)
            {
                DisableUpdates();
                isLoading = true;
                SendCommand(NET_COMMAND_RDP_OPEN);
                Core.Scheduler.PushDelayedTask(() => EnableUpdates(), config.StartupTime);
            }
        }
        public override void OnServerStart(EventArgs e)
        {
            if (steamMode && config.Enable)
            {
                if (((StartEventArgs)e).Attached) EnableUpdates();

                try
                {
                    MemoryInit();
                }
                catch
                {
                    MemoryClose();
                    Logger.Log(LogLevel.Warning, "IngameServerController failed to attach. Server won't be supported.");
                }
            }
        }
        public override void OnServerStop()
        {
            if (config.Enable)
            {
                DisableUpdates();
                MemoryClose();
            }
        }
        private void HandleStatus()
        {
            if (ReadMapStatus() != 0)
            {
                if (!isLoading)
                {
                    SendCommand(NET_COMMAND_RDP_OPEN);
                }
                isLoading = true;
            }
            else
            {
                if (isLoading)
                {
                    isLoading = false;
                    SendCommand(NET_COMMAND_RDP_CLOSE);
                }
            }
        }
        protected override void OnUpdate()
        {
            try
            {
                HandleStatus();
            }
            catch
            {
                //TODO
            }
        }
        ~IngameServerController()
        {
            OnServerStop(); //make sure we close any open handle
        }
    }
}