using System;

namespace DllLoader
{
    class Program
    {
        const string ARG_PID = "--pid";
        const string ARG_DLL = "--dll";

        static void Main(string[] args)
        {
            int pid = -1;
            string dll = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(ARG_PID))
                {
                    if (args.Length < i + 2 || !int.TryParse(args[++i], out pid))
                    {
                        Console.WriteLine("Invalid argument for {0} - exiting", ARG_PID);
                        return;
                    }
                }

                else if (args[i].Equals(ARG_DLL))
                {
                    if (args.Length < i + 2)
                    {
                        Console.WriteLine("Invalid argument for {0} - exiting", ARG_DLL);
                        return;
                    }
                    dll = args[++i];
                }
            }

            if(pid < 0 || dll == null)
            {
                Console.WriteLine("Usage: dllloader.exe {0} <pid> {1} <dll name>", ARG_PID, ARG_DLL);
                return;
            }

            Console.Write("Injecting dll...");
            try
            {
                DllInjector.Inject(pid, dll);
                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(e.ToString());
            }
        }
    }
}