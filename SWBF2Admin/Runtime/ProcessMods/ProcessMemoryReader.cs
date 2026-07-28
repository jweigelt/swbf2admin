using SWBF2Admin.Utility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SWBF2Admin.Runtime.Readers
{
    public class ProcessMemoryReader : IDisposable
    {
        #region imports
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
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        #endregion

        private readonly object handleLock = new object();
        private IntPtr hProc = IntPtr.Zero;
        private IntPtr moduleBase;
        public bool IsProcessOpen { get; private set; } = false;
        public int? ProcessId { get; private set; }
        public string LastOpenError { get; private set; }

        public IntPtr GetModuleBase(long offset)
        {
            return new nint(moduleBase.ToInt64() + offset);
        }

        private int targetPointerSize = 4;

        public bool IsTarget64Bit => targetPointerSize == 8;

        public void SetTargetPointerSize(int size)
        {
            if (size != 4 && size != 8)
            {
                throw new ArgumentException("Pointer size must be either 4 or 8 bytes.");
            }
            targetPointerSize = size;
        }

        #region open
        public bool Open(Process process, string moduleName)
        {
            Close();
            LastOpenError = null;

            if (process == null)
            {
                LastOpenError = "The server process is not available.";
                return false;
            }

            IntPtr openedHandle = IntPtr.Zero;

            try
            {
                int processId = process.Id;
                process.Refresh();
                if (process.HasExited)
                {
                    LastOpenError = "The server process exited while attaching.";
                    return false;
                }

                IntPtr foundModuleBase;

                if (string.IsNullOrEmpty(moduleName))
                {
                    ProcessModule mainModule = process.MainModule;
                    if (mainModule == null)
                    {
                        LastOpenError = "The process main module is not available yet.";
                        return false;
                    }

                    foundModuleBase = mainModule.BaseAddress;
                }
                else
                {
                    ProcessModule targetModule = null;

                    foreach (ProcessModule module in process.Modules)
                    {
                        if (string.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetModule = module;
                            break;
                        }
                    }

                    if (targetModule == null)
                    {
                        LastOpenError = string.Format("Module \"{0}\" is not loaded yet.", moduleName);
                        return false;
                    }

                    foundModuleBase = targetModule.BaseAddress;
                }

                const ProcessAccessFlags requiredAccess =
                    ProcessAccessFlags.VirtualMemoryOperation |
                    ProcessAccessFlags.VirtualMemoryRead |
                    ProcessAccessFlags.VirtualMemoryWrite |
                    ProcessAccessFlags.QueryInformation;

                openedHandle = OpenProcess(requiredAccess, false, processId);
                if (openedHandle == IntPtr.Zero)
                {
                    LastOpenError = new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcess failed.").Message;
                    return false;
                }

                lock (handleLock)
                {
                    hProc = openedHandle;
                    openedHandle = IntPtr.Zero;
                    moduleBase = foundModuleBase;
                    ProcessId = processId;
                    IsProcessOpen = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                //A newly started process may not expose all modules immediately
                LastOpenError = ex.Message;
                return false;
            }
            finally
            {
                if (openedHandle != IntPtr.Zero)
                    CloseHandle(openedHandle);
            }
        }

        public void Open(int pid)
        {
            try
            {
                using Process proc = Process.GetProcessById(pid);
                if (!Open(proc, null))
                    throw new Exception(LastOpenError ?? "OpenProcess failed.");
            }
            catch (Exception ex)
            {
                Close();
                throw new Exception($"No process found for id: {pid}", ex);
            }
        }

        public bool Open(Process process)
        {
            return Open(process, null);
        }

        public bool Open(string name)
        {
            try
            {
                Process[] procs = Process.GetProcessesByName(name);

                foreach (Process proc in procs)
                {
                    using (proc)
                    {
                        if (Open(proc, null))
                            return true;
                    }
                }

                LastOpenError ??= string.Format("No process named \"{0}\" is available.", name);
                return false;
            }
            catch (Exception ex)
            {
                LastOpenError = ex.Message;
                Close();
                return false;
            }
        }

        public void Close()
        {
            IntPtr handle;

            lock (handleLock)
            {
                handle = hProc;
                hProc = IntPtr.Zero;
                moduleBase = IntPtr.Zero;
                ProcessId = null;
                IsProcessOpen = false;
            }

            if (handle != IntPtr.Zero)
                CloseHandle(handle);
        }
        #endregion

        #region read
        public byte[] ReadBytes(IntPtr address, int length)
        {
            byte[] buf = new byte[length];
            ReadProcessMemory(hProc, address, buf, buf.Length, out _);
            return buf;
        }
        public float ReadFloat(IntPtr address)
        {
            byte[] buf = new byte[4];
            ReadProcessMemory(hProc, address, buf, 4, out IntPtr read);
            return BitConverter.ToSingle(buf, 0);
        }
        public int ReadInt32(IntPtr address)
        {
            byte[] buf = new byte[4];
            ReadProcessMemory(hProc, address, buf, 4, out IntPtr read);
            return BitConverter.ToInt32(buf, 0);
        }
        public int ReadInt16(IntPtr address)
        {
            byte[] buf = new byte[2];
            ReadProcessMemory(hProc, address, buf, 2, out IntPtr read);
            return BitConverter.ToInt16(buf, 0);
        }
        public int ReadInt8(IntPtr address)
        {
            byte[] buf = new byte[1];
            ReadProcessMemory(hProc, address, buf, 1, out IntPtr read);
            return Convert.ToByte(buf[0]);
        }
        public string ReadWString(IntPtr address, int len)
        {
            len *= 2;
            byte[] buf = new byte[len];
            ReadProcessMemory(hProc, address, buf, len, out IntPtr read);
            int strLen = 0;
            for (int i = 0; i < len; i += 2)
            {
                if (buf[i] == 0)
                {
                    strLen = i;
                    break;
                }
            }
            return Encoding.Unicode.GetString(buf, 0, strLen);
        }

        public string ReadString(IntPtr address, int len)
        {
            byte[] buf = new byte[len];
            ReadProcessMemory(hProc, address, buf, len, out IntPtr read);
            int strLen = 0;
            for (int i = 0; i < len; i++)
            {
                if (buf[i] == 0)
                {
                    strLen = i;
                    break;
                }
            }
            return Encoding.ASCII.GetString(buf, 0, strLen);
        }

        public IntPtr ReadPtr(IntPtr address)
        {
            byte[] buf = new byte[targetPointerSize];

            if (!ReadProcessMemory(hProc, address, buf, buf.Length, out IntPtr read) || read.ToInt64() != buf.Length)
            {
                int err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err, $"ReadPtr failed at 0x{address.ToInt64():X}");
            }

            return targetPointerSize == 8
                ? new IntPtr(BitConverter.ToInt64(buf, 0))
                : new IntPtr(BitConverter.ToInt32(buf, 0));
        }
        #endregion

        #region write
        public void WriteBytes(IntPtr address, byte[] data)
        {
            if (!WriteProcessMemory(hProc, address, data, (uint)data.Length, out UIntPtr written))
            {
                Console.WriteLine("hProc -> {0}", hProc);
                throw new Win32Exception();
            }
        }
        public void WriteFloat(IntPtr address, float value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            if (!WriteProcessMemory(hProc, address, buf, 4, out UIntPtr written))
            {
                Console.WriteLine("hProc -> {0}", hProc);
                throw new Win32Exception();
            }
        }
        public void WriteInt32(IntPtr address, int value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            if (!WriteProcessMemory(hProc, address, buf, 4, out UIntPtr written))
            {
                Console.WriteLine("hProc -> {0}", hProc);
                throw new Win32Exception();
            }
        }
        public void WriteInt16(IntPtr address, int value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            if (!WriteProcessMemory(hProc, address, buf, 2, out UIntPtr written))
            {
                Console.WriteLine("hProc -> {0}", hProc);
                throw new Win32Exception();
            }
        }
        public void WriteInt8(IntPtr address, int value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            if (!WriteProcessMemory(hProc, address, buf, 1, out UIntPtr written))
            {
                Console.WriteLine("hProc -> {0}", hProc);
                throw new Win32Exception();
            }
        }
        #endregion

       
        
        public IntPtr AllocateMemory(int caveSize)
        {
            var caveAddress = VirtualAllocEx(hProc, (IntPtr)null, caveSize, 0x1000 | 0x2000, 0x40);
            return caveAddress;
        }
        public bool FreeMemory(IntPtr caveAddress)
        {
            return VirtualFreeEx(hProc, caveAddress, 0, 0x8000);
        }

        /*
            * basePtr    - starting address to add offsets to
            * offsets    - array of offsets for pointers
            */
        public IntPtr GetOffsetIntPtr(IntPtr basePtr, int[] offsets)
        {
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                basePtr = ReadPtr(IntPtr.Add(basePtr, offsets[i]));
            }

            IntPtr address = IntPtr.Add(basePtr, offsets[offsets.Length - 1]);

            return address;
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        ~ProcessMemoryReader()
        {
            Close();
        }
    }
}
