using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Utilities.Extensions;

//using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.EfCoreTool.Generator;

internal abstract class CommandBase
{
    protected const string CachedEntityFolder = "CacheModels";
    protected const string CacheEntityClassType = "CacheModel";
    internal const string AbstractionsFolderName = "Abstractions";
    internal const string RepositoriesFolder = "Repositories";
    private const string ResourceNamePrefix = "Solhigson.Framework.EfCoreTool.Templates.";
    protected static readonly List<string> ValidOptions = [AssemblyPathOption, DatabaseContextName];
    private const string AssemblyPathOption = "-a";
    private const string DatabaseContextName = "-dc";
    protected const string RootNamespaceOption = "-rn";
    internal string RepositoryNamespace { get; set; }
    internal string CachedEntityNamespace { get; set; }

    protected string PersistenceProjectRootNamespace { get; private set; }
    protected string ApplicationName { get; private set; }
    protected string ProjectRootNamespace { get; set; }
    private string DbContextNamespace { get; set; }
    private string DbContextName { get; set; }

    protected string ServicesFolder { get; set; }


    protected string DtoProjectNamespace { get; set; }
    protected string ContractsProjectNamespace { get; set; }

    protected CommandBase()
    {
        Args = new Dictionary<string, string>();
    }

    protected Dictionary<string, string> Args { get; }

    internal (bool IsValid, string ErrorMessage) ParseArguments(string[] args)
    {
        Console.WriteLine("Args: ");
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

            Console.WriteLine($"{option} => {value}");
            Args.Add(option, value);
        }

        return ValidateAll();
    }

    internal IList<Type?> Models { get; private set; }
    internal string AssemblyFolderPath { get; set; }

    internal abstract void Run();
    internal abstract string CommandName { get; }

    private (bool IsValid, string ErrorMessage) ValidateAll()
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            if (!Args.TryGetValue(AssemblyPathOption, out var assemblyPath))
            {
                return (false, "Assembly path is required [-a <path>]");
            }

            if (!File.Exists(assemblyPath))
            {
                return (false, $"Invalid assembly file path: {assemblyPath}");
            }

            AssemblyFolderPath = new FileInfo(assemblyPath).DirectoryName;

            var assembly = Assembly.LoadFile(assemblyPath);
            var databaseContexts = assembly
                .GetTypes().Where(t => t.IsSubclassOf(typeof(DbContext))).ToList();
            Console.WriteLine("Database Contexts found: ");
            foreach (var dbContext in databaseContexts)
            {
                Console.WriteLine(dbContext.Name);
            }

            if (!Args.TryGetValue(RootNamespaceOption, out var rootNamespace))
            {
                return (false, $"Root namespace is required via arg: {RootNamespaceOption}");
            }

            ProjectRootNamespace = rootNamespace;

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

            Console.WriteLine($"Using databaseContext: {databaseContext}");

            Models = databaseContext.GetProperties(BindingFlags.DeclaredOnly |
                                                   BindingFlags.Public |
                                                   BindingFlags.Instance).Where(t =>
                    t.PropertyType.IsDbSetType())
                .Select(t => t.PropertyType.GetGenericArguments()[0]).ToList();

            if (!Models.Any())
            {
                return (false,
                    $"Database Context: [{databaseContext.FullName}] does not have any properties of type DbSet<>");
            }

            PersistenceProjectRootNamespace = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(PersistenceProjectRootNamespace))
            {
                return (false, "Couldn't retrieve assembly name");
            }

            DbContextName = databaseContext.Name;
            DbContextNamespace = databaseContext.Namespace;
            if (ProjectRootNamespace.Contains("."))
            {
                var split = ProjectRootNamespace.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                ApplicationName = split[0];
            }
            else
            {
                ApplicationName = ProjectRootNamespace;
            }
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }

        return Validate();
    }

    private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var path = $"{AssemblyFolderPath}/{args.Name.Split(',')[0] + ".dll".ToLower()}";
        Console.WriteLine($"Resolving & loading assembly: {path}");
        return Assembly.LoadFile(path);
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

    protected abstract (bool IsValid, string ErrorMessage) Validate();

    protected void GenerateFile(string rootPath, string folder, string type,
        string? entityName, string entityNamespace,
        bool isInterface, bool isGenerated, string properties = "", bool isCachedEntity = false)
    {
        var interfaceIndicator = isInterface ? "I" : "";
        var abstractionsFolder = isInterface ? $"/{AbstractionsFolderName}" : "";
        var generatedIndicator = isGenerated ? ".generated" : "";

        var pathSafeFolder = folder;
        if (folder?.Contains(".") == true)
        {
            pathSafeFolder = folder.Replace(".", "/");
        }

        var path =
            $"{rootPath}/{pathSafeFolder}{abstractionsFolder}/{interfaceIndicator}{entityName}{type}{generatedIndicator}.cs";
        var placeHolderName = "Placeholder";
        var cachedRepositoryIndicator = "";
        var cachedRepositoryClassPrefix = "";
        if (isCachedEntity)
        {
            cachedRepositoryIndicator = "Cached";
            cachedRepositoryClassPrefix =
                $",{ContractsProjectNamespace}.{CachedEntityNamespace}.{entityName}{CacheEntityClassType}";
        }

        var resourcePath =
            $"{ResourceNamePrefix}{interfaceIndicator}{placeHolderName}{type}{generatedIndicator}.cs";
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
        else if (!string.IsNullOrWhiteSpace(entityNamespace))
        {
            entityNamespaceDeclaration = $"using {entityNamespace};";
        }

        var resource = reader.ReadToEnd().Replace("[Placeholder]", entityName)
            .Replace("[ProjectRootNamespace]", ProjectRootNamespace)
            .Replace("[PersistenceProjectRootNamespace]", PersistenceProjectRootNamespace)
            .Replace("[Folder]", folder)
            .Replace("[CacheEntityNamespace]", CachedEntityNamespace)
            // .Replace("[Folder]", folder)
            .Replace("[ServicesFolder]", ServicesFolder)
            .Replace("[DbContextName]", DbContextName)
            .Replace("[DbContextNamespace]", DbContextNamespace)
            .Replace("[EntityNameSpaceDeclaration]", entityNamespaceDeclaration)
            .Replace("[EntityNameSpace]", entityNamespace)
            .Replace("[Properties]", properties)
            .Replace("[ApplicationName]", ApplicationName)
            .Replace("[AbstractionsFolder]", AbstractionsFolderName)
            .Replace("[RepositoriesFolder]", RepositoryNamespace)
            .Replace("[DtoProjectNamespace]", DtoProjectNamespace)
            .Replace("[ContractsProjectNamespace]", ContractsProjectNamespace)
            .Replace("[Cached]", cachedRepositoryIndicator)
            .Replace("[CachedEntityModel]", cachedRepositoryClassPrefix)
            .Replace("[CustomFileComment]", GetComment("This file is never overwritten, place custom code here"))
            .Replace("[GeneratedFileComment]",
                GetComment("This file is ALWAYS overwritten, DO NOT place custom code here", false));
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

    private static string GetComment(string comment, bool includeDate = true)
    {
        var date = "";
        if (includeDate)
        {
            date = $"\n\t * Generated on: {DateTime.UtcNow:dd-MMM-yyyy HH:mm:ss UTC}";
        }

        return $@"/*{date}
     * Generated by: Solhigson.Framework.efcoretool
     *
     * https://github.com/solhigson-public/solhigson.framework
     * https://www.nuget.org/packages/solhigson.framework.efcoretool
     *
     * {comment}
     */";
    }
    


}