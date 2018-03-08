using System;

namespace DllLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Injecting dll...");
            try
            {
                DllInjector.Inject("BattlefrontII", "RconServer.dll");
                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed.");
                Console.WriteLine(e.ToString());
            }

#if DEBUG
            Console.WriteLine("Press [return] to exit.");
            Console.ReadLine();
#endif
        }
    }
}