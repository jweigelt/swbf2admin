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
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SWBF2Admin.Gameserver
{
    public class IngameServerController : ComponentBase
    {
        public const string SETTING_SPAWNTIMER = "mods";
        private const string DLL_LOADER = "dllloader.exe";

        private const int OFFSET_MAP_STATUS = (0x01EAFCA0 - 0x00401000 + 0x1000);
        private const int OFFSET_NORENDER = (0x01EAD47B - 0x00401000 + 0x1000);
        
        private const int OFFSET_MAP_STATUS_GOG = (0x01EB1054 - 0x00401000 + 0x1000);//(0x01B21054);


        private const int OFFSET_MAPFIX_STATUS_GOG = (0x1E6433F - 0x00401000 + 0x1000);
        private const int OFFSET_SPAWNTIMER_GOG = (0x007E8FE8 - 0x00401000 + 0x1000);

        public event EventHandler GameEnded;

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

        private bool enableRuntime;
        private bool isLoading = false;     //map load in progress
        private bool steamMode;             //steam mode enabled?
        private bool gogMode;				//gog mode enabled?
        private int notRespondingCount = 0; //times the server process didn't respond
        private int mapHangTime = 0;        //time since game ended
        private int freezeCount = 0;        //times we tried to freeze-unfreez

        private IngameServerControllerConfiguration config;

        private IntPtr moduleBase;
        private IntPtr procHandle = IntPtr.Zero;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        public IngameServerController(AdminCore core) : base(core) { }

        public override void OnInit()
        {
            if (steamMode || gogMode)
            {
                UpdateInterval = config.MapCheckInterval;
                Core.Scheduler.PushRepeatingTask(() => CheckResponding(), config.NotRespondingCheckInterval);
            }
        }

        public override void OnServerStart(EventArgs e)
        {
            if (steamMode || gogMode)
            {
                if (((StartEventArgs)e).Attached) EnableUpdates();
                try
                {
                    MemoryInit();
                    Core.Scheduler.PushDelayedTask(() => EnableUpdates(), config.StartupTime);
                }
                catch
                {
                    MemoryClose();
                    Logger.Log(LogLevel.Warning, "IngameServerController failed to attach. Server won't be supported.");
                }

                if (enableRuntime && !((StartEventArgs)e).Attached)
                {
                    Logger.Log(LogLevel.Info, "Attach RconServer.dll to server process...");
                    try
                    {
                        string loader = $"{Core.Files.ParseFileName(Core.Config.ServerPath)}/{DLL_LOADER}";
                        if (File.Exists(loader))
                        {
                            Process.Start(loader);
                        }

                        //TODO: messy workaround to give RconServer time to start before RconClient connects
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "Failed to load dll : {0}", ex.Message);
                    }
                }
            }
        }
        public override void OnServerStop()
        {
            if (steamMode || gogMode)
            {
                DisableUpdates();
                MemoryClose();
            }
        }
        protected override void OnUpdate()
        {
            try
            {
                CheckMapStatus();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Verbose, e.Message);
            }
        }

        public override void Configure(CoreConfiguration config)
        {
            steamMode = config.EnableSteamMode;
            gogMode = config.EnableGOGMode;
            enableRuntime = config.EnableRuntime;
            this.config = Core.Files.ReadConfig<IngameServerControllerConfiguration>();

            //TODO: clean that up:
            //calling getter once so any format errors are thrown now (during config) and not during runtime
            IPEndPoint ipep = this.config.ServerIPEP;
        }


        private byte ReadMapStatus()
        {
            if (steamMode)
            {
                return ReadByte(OFFSET_MAP_STATUS);
            }
            else
            {
                return ReadByte(OFFSET_MAP_STATUS_GOG);
            }
        }
        private byte ReadByte(int offset)
        {
            IntPtr bytesRead;
            byte[] buf = new byte[1];
            if (!ReadProcessMemory(procHandle, IntPtr.Add(moduleBase, offset), buf, 1, out bytesRead) || bytesRead == IntPtr.Zero)
                throw new Exception("ReadProcessMemory() failed");
            return buf[0];
        }
        private void WriteByte(int offset, byte value)
        {
            UIntPtr bytesWritten;
            byte[] buf = new byte[] { value };
            if (!WriteProcessMemory(procHandle, IntPtr.Add(moduleBase, offset), buf, 1, out bytesWritten) || bytesWritten == UIntPtr.Zero)
                throw new Exception("WriteProcessMemory() failed");

        }
        private void WriteFloat(int offset, float value)
        {
            UIntPtr bytesWritten;
            byte[] buf = BitConverter.GetBytes(value);
            if (!WriteProcessMemory(procHandle, IntPtr.Add(moduleBase, offset), buf, 4, out bytesWritten) || bytesWritten == UIntPtr.Zero)
                throw new Exception("WriteProcessMemory() failed");
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

        private void CheckResponding()
        {
            //check if the process is stuck and "crash" it manually if necessary
            Process p = Core.Server.ServerProcess;
            if (p != null && !p.HasExited)
            {
                if (!p.Responding)
                {
                    if (notRespondingCount++ >= config.NotRespondingMaxCount)
                        p.Kill();
                }
                else notRespondingCount = 0;
            }
        }

        private void CheckMapStatus()
        {
            if (ReadMapStatus() != 0)
            {
                if (!isLoading)
                {
                    isLoading = true;
                    InvokeEvent(GameEnded, this, new EventArgs());
                }
            }
            else
            {
                byte b = ReadByte(OFFSET_MAPFIX_STATUS_GOG);
                if (isLoading)
                {
                    WriteByte(OFFSET_MAPFIX_STATUS_GOG, 0);
                    Logger.Log(LogLevel.Info, "Server finished loading - mapfix status: {0}", b.ToString());
                    isLoading = false;
                }else if (b > 0) {
                    WriteByte(OFFSET_MAPFIX_STATUS_GOG, 0);
                    Logger.Log(LogLevel.Warning, "Missed map reload - resetting mapfix");
                }
            }
        }
        ~IngameServerController()
        {
            OnServerStop(); //make sure we close any open handle
        }
    }
}