using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;

namespace Solhigson.Framework.Tools
{
    internal abstract class CommandBase
    {
        internal const string AbstractionsFolderName = "Abstractions";
        internal const string ResourceNamePrefix = "Solhigson.Framework.Tools.Templates.";
        protected static readonly List<string> ValidOptions = new() { AssemblyPathOption, DatabaseContextName };
        protected const string AssemblyPathOption = "-a";
        protected const string DatabaseContextName = "-d";
        protected string Namespace { get; private set; }
        protected string ApplicationName { get; private set; }
        protected string DbContextNamespace { get; private set; }
        protected string DbContextName { get; private set; }

        protected CommandBase()
        {
            Args = new Dictionary<string, string>();
        }
        protected Dictionary<string, string> Args { get; }
        internal (bool IsValid, string ErrorMessage) ParseArguments(string[] args)
        {
            for (var i = 1; i < args.Length; i++)
            {
                var option = args[i];
                var value = args[i + 1];
                if (!value.StartsWith("-"))
                {
                    i++;
                }
                else
                {
                    value = "";
                }
                Args.Add(option, value);
            }

            return ValidateAll();
        }

        internal IList<Type> Models { get; private set; }
        
        internal abstract void Run();
        internal abstract string CommandName { get; }

        private (bool IsValid, string ErrorMessage) ValidateAll()
        {
            try
            {
                if (!Args.TryGetValue(AssemblyPathOption, out var assemblyPath))
                {
                    return (false, "Assembly path is required [-a <path>]");
                }

                if (!File.Exists(assemblyPath))
                {
                    return (false, $"Invalid assembly file path: {assemblyPath}");
                }
                var assembly = Assembly.LoadFile(assemblyPath);
                var databaseContexts = assembly
                    .GetTypes().Where(t => t.IsSubclassOf(typeof(DbContext))).ToList();

                if (databaseContexts.Any() == false)
                {
                    return (false, $"No database Contexts found");
                }

                Type databaseContext;
                var dbContextNameSpecified = Args.TryGetValue(DatabaseContextName, out var dbContextName);

                if (databaseContexts.Count > 1)
                {
                    if (!dbContextNameSpecified)
                    {
                        return (false, $"Multiple database contexts found, specify with [-d <contextName>]");
                    }

                    if (databaseContexts.All(t => t.Name != dbContextName))
                    {
                        return (false, $"Specified database context: [{dbContextName}] not found in assembly");
                    }
                    databaseContext = databaseContexts.FirstOrDefault(t => t.Name == dbContextName);
                }
                else
                {
                    if (dbContextNameSpecified && databaseContexts.All(t => t.Name != dbContextName))
                    {
                        return (false, $"Specified database context: [{dbContextName}] not found in assembly");
                    }
                    databaseContext = databaseContexts.FirstOrDefault();
                }

                if (databaseContext == null)
                {
                    return (false, $"No database Contexts found");
                }


                Models = databaseContext.GetProperties().Where(t => t.PropertyType.IsGenericType && t.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .Select(t => t.PropertyType.GetGenericArguments()[0]).ToList();

                if (!Models.Any())
                {
                    return (false, $"Database Context: [{databaseContext.FullName}] has not properties of type DbSet<>");
                }
                Namespace = assembly.GetName().Name;
                DbContextName = databaseContext.Name;
                DbContextNamespace = databaseContext.Namespace;
                if (Namespace.Contains("."))
                {
                    var split = Namespace.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
                    ApplicationName = split[0];
                }
                else
                {
                    ApplicationName = Namespace;
                }

            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
            return Validate();
        }

        internal void Display()
        {
            Console.WriteLine($"Command: {GetType().Name}");
            Console.WriteLine("-----------------");
            Console.WriteLine("Args: ");
            foreach (var key in Args)
            {
                Console.WriteLine($"{key.Key} = {key.Value}");
            }
            Console.WriteLine("-----------------");
            Console.WriteLine("Models: ");
            foreach (var type in Models)
            {
                Console.WriteLine($"{type.FullName}");
            }
        }
        
        internal abstract (bool IsValid, string ErrorMessage) Validate();
        
        protected void GenerateFile(string rootPath, string folder, string type, 
            string entityName, string entityNamespace, 
            bool isInterface, bool isGenerated, string properties = "")
        {
            var interfaceIndicator = isInterface ? "I" : "";
            var abstractionsFolder = isInterface ? $"/{AbstractionsFolderName}" : "";
            var generatedIndicator = isGenerated ? ".generated" : "";
            
            var path = $"{rootPath}/{folder}{abstractionsFolder}/{interfaceIndicator}{entityName}{type}{generatedIndicator}.cs";
            var placeHolderName = "Placeholder";
            /*
            if (type == "Wrapper")
            {
                placeHolderName = entityName;
            }
            */
            var resourcePath = $"{ResourceNamePrefix}{interfaceIndicator}{placeHolderName}{type}{generatedIndicator}.cs";
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream is null)
            {
                Console.WriteLine($"Resource not found: {resourcePath}");
                return;
            }

            var entityNamespaceDeclaration = "";
            using var reader = new StreamReader(stream);
            if (entityNamespace == DbContextNamespace)
            {
                entityNamespaceDeclaration = "";
            }
            else if(!string.IsNullOrWhiteSpace(entityNamespace))
            {
                entityNamespaceDeclaration = $"using {entityNamespace};";
            }
            var resource = reader.ReadToEnd().Replace("[Placeholder]", entityName)
                .Replace("[Namespace]", Namespace).Replace("[Folder]", folder)
                .Replace("[DbContextName]", DbContextName).Replace("[DbContextNamespace]", DbContextNamespace)
                .Replace("[EntityNameSpaceDeclaration]", entityNamespaceDeclaration)
                .Replace("[EntityNameSpace]", entityNamespace)
                .Replace("[Properties]", properties)
                .Replace("[ApplicationName]", ApplicationName)
                .Replace("[AbstractionsFolder]", AbstractionsFolderName)
                .Replace("[CustomFileComment]", "This file is never overwritten, place custom code here")
                .Replace("[GeneratedFileComment]", "This file is ALWAYS overwritten, DO NOT place custom code here");
            SaveFile(resource, path);
        }
        
        private static void SaveFile(string file, string path)
        {
            Console.WriteLine($"Attempting to generate path: {path}");
            var directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            if (!path.Contains(".generated.cs") && File.Exists(path))
            {
                Console.WriteLine($"Skipping");
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using var fileStream = File.OpenWrite(path);
            using var streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(file);
            Console.WriteLine($"Generated: {path}");
        }


    }
}