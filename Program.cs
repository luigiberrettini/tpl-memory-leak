using System;
using System.IO;
using System.Reflection;

namespace ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // http://github.com/dotnet/project-system/issues/2239
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            var asyncLogger = new AsyncLogger();

            while (true)
            {
                try
                {
                    var times = PromptUserForOperationChoice();
                    if (times == 0)
                    {
                        asyncLogger.Dispose();
                        return;
                    }
                    for (int i = 0; i < times; i++)
                        asyncLogger.Log(DateTime.UtcNow.ToString("o"));
                }
                catch
                {
                    Console.WriteLine("Invalid choice");
                }
                Console.WriteLine();
            }
        }

        public static int PromptUserForOperationChoice()
        {
            Console.WriteLine("Choose the number of messages:");
            var choice = Console.ReadLine();
            Console.WriteLine();
            return int.Parse(choice);
        }
    }
}