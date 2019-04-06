using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DllLoader
{
    class DllInjector
    {
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        public static void Inject(int pid, string dllName)
        {
            Process proc = Process.GetProcessById(pid);

            if(proc == null)
            {
                throw new Exception(string.Format("No process matching id {0} found.", pid));          
            }

            IntPtr hProc = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, proc.Id);
            if(hProc == IntPtr.Zero)
            {
                throw new Exception("OpenProcess() failed.");
            }

            IntPtr hLL = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (hLL == IntPtr.Zero)
            {
                throw new Exception("GetProcAddress() failed.");
            }

            IntPtr allocMemAddress = VirtualAllocEx(hProc, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero)
            {
               throw new Exception("VirtualAllocEx() failed.");
            }

            UIntPtr bytesWritten;
            WriteProcessMemory(hProc, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);

            if (bytesWritten == UIntPtr.Zero)
            {
                throw new Exception("WriteProcessMemory() failed.");
            }

            CreateRemoteThread(hProc, IntPtr.Zero, 0, hLL, allocMemAddress, 0, IntPtr.Zero);
        }
    }
}