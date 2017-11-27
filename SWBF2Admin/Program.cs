using System;
namespace SWBF2Admin
{
    class Program
    {
        static void Main(string[] args)
        {
            AdminCore core;
            try
            {
                core = new AdminCore();
            }
            catch 
            {
                return;
            }

            core.Run();
        }
    }
}
