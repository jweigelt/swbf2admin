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

        public static void Inject(string processName, string dllName)
        {
            Process[] procs = Process.GetProcessesByName(processName);
            if(procs.Length < 1)
            {
                Console.WriteLine("No process matching {0} found.",processName);
                return;
            }
            Process proc = procs[0];
            IntPtr hProc = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, proc.Id);
            if(hProc == IntPtr.Zero)
            {
                Console.WriteLine("OpenProcess() failed.");
                return;
            }

            IntPtr hLL = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (hLL == IntPtr.Zero)
            {
                Console.WriteLine("GetProcAddress() failed.");
                return;
            }

            IntPtr allocMemAddress = VirtualAllocEx(hProc, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero)
            {
                Console.WriteLine("VirtualAllocEx() failed.");
                return;
            }

            UIntPtr bytesWritten;
            WriteProcessMemory(hProc, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);

            if (bytesWritten == UIntPtr.Zero)
            {
                Console.WriteLine("WriteProcessMemory() failed.");
                return;
            }

            CreateRemoteThread(hProc, IntPtr.Zero, 0, hLL, allocMemAddress, 0, IntPtr.Zero);
        }
    }
}