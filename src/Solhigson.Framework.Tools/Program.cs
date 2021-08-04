using System;
using System.Reflection;

namespace Solhigson.Framework.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion
                    .ToString();

                Console.WriteLine($"Solhigson.Tools v{versionString}");
                Console.WriteLine("-------------");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  solhigson <message>");
            }
            
            ShowBot(string.Join(' ', args));
        }
        
        static void ShowBot(string message)
        {
            string bot = $"\n        {message}";
            bot += @"
   _____       ____    _                      
  / ___/____  / / /_  (_)___ __________  ____ 
  \__ \/ __ \/ / __ \/ / __ `/ ___/ __ \/ __ \
 ___/ / /_/ / / / / / / /_/ (__  ) /_/ / / / /
/____/\____/_/_/ /_/_/\__, /____/\____/_/ /_/ 
  ______            _/____/                   
 /_  __/___  ____  / /____                    
  / / / __ \/ __ \/ / ___/                    
 / / / /_/ / /_/ / (__  )                     
/_/  \____/\____/_/____/                      
                                              ";
            Console.WriteLine(bot);
        }
    }
}