using System;

namespace WikipediaConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Test().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
