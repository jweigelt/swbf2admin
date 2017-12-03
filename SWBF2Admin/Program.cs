using System;
namespace SWBF2Admin
{
    class Program
    {
        static void Main(string[] args)
        {
            new AdminCore().Run();
#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}