using System;

namespace Solhigson.Framework.Tools.Generator
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
            //var rootPath = 
            foreach (var entity in Models)
            {
                
            }
            Console.WriteLine("Running...");
        }
        


    }
}