using System;
using System.Collections.Generic;
using Solhigson.Framework.EfCoreTool.Generator;

namespace Solhigson.Framework.EfCoreTool
{
    public class CommandWrapper
    {
        private const string GenerateCommand = "gen";
        internal static List<string> ValidCommands = new() {GenerateCommand};
        
        private CommandBase Command { get; }
        internal string CommandName { get; set; }
        internal string ErrorMessage { get; set; }
        internal bool IsValid { get; set; }
        internal CommandWrapper(string []args)
        {
            var command = args[0];
            if (!ValidCommands.Contains(command))
            {
                ErrorMessage = $"Unrecognised command: {command}";
                return;
            }

            Command = command switch
            {
                _ => new GenCommand()
            };
            
            var (isValid, errorMessage) = Command.ParseArguments(args);

            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        internal void Display()
        {
            Command.Display();
        }

        internal void Run()
        {
            try
            {
                Command.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("***Tool ending prematurely***");
            }
        }
    }
}