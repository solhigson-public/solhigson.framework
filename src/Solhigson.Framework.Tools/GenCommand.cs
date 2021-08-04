using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solhigson.Framework.Tools
{
    internal class GenCommand : CommandBase
    {
        private const string RepositoryDirectoryOption = "-rd";
        private const string ServicesDirectoryOption = "-sd";
        private const string RepositoryDirectory = "Data\\Repositories";
        private const string ServicesDirectory = "Services";

        internal override string CommandName => "Gen";
        
        internal override (bool IsValid, string ErrorMessage) Validate()
        {
            ValidOptions.Add(RepositoryDirectoryOption);
            ValidOptions.Add(ServicesDirectoryOption);
            
            foreach (var key in Args)
            {
                if (!ValidOptions.Contains(key.Key))
                {
                    return (false, $"Invalid option: {key.Key}");
                }
            }

            return (true, "");
        }
        
        internal override void Run()
        {
            Console.WriteLine("Running...");
        }

    }
}