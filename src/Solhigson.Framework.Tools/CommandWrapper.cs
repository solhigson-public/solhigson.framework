﻿using System;
using System.Collections.Generic;

namespace Solhigson.Framework.Tools
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
            Command.Run();
        }
    }
}