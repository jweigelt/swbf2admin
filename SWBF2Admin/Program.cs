using System;
using SWBF2Admin.Utility;
namespace SWBF2Admin
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            new AdminCore().Run(args);
            Console.ReadLine();
#else
            try
            {
                new AdminCore().Run();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Exiting ({0})", e.Message);
            }
#endif
        }
    }
}