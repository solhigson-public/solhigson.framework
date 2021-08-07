using System;
using System.Reflection;

namespace Solhigson.Framework.EfCoreTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);
            if (args.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion
                    .ToString();

                Console.WriteLine($"Solhigson.Tools v{versionString}");
                Console.WriteLine("-------------");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  solhigson <command> <args>");
                return;
            }

            var commandWrapper = new CommandWrapper(args);
            if (!commandWrapper.IsValid)
            {
                Console.WriteLine(commandWrapper.ErrorMessage);
                return;
            }
            //ShowBot(string.Join(' ', args));
            ShowBot();
            commandWrapper.Display();
            commandWrapper.Run();
        }
        
        static void ShowBot(string message = null)
        {
            string bot = $"\n        {message}";
            bot += @"
   _____       ____    _                      
  / ___/____  / / /_  (_)___ __________  ____ 
  \__ \/ __ \/ / __ \/ / __ `/ ___/ __ \/ __ \
 ___/ / /_/ / / / / / / /_/ (__  ) /_/ / / / /
/____/\____/_/_/ /_/_/\__, /____/\____/_/ /_/ 
                    /____/                   
                                              ";
            Console.WriteLine(bot);
        }
    }
}