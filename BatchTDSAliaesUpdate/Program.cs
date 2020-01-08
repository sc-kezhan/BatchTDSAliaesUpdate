using System;

namespace BatchTDSAliaesUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            var projectFilePath = args[0];
            try
            {
                var service = new UpdateService();
                service.Process(projectFilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
