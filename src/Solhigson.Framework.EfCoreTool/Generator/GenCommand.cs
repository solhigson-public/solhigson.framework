using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Repository;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.EfCoreTool.Generator
{
    internal class GenCommand : CommandBase
    {
        private const string RepositoryDirectoryOption = "-rd";
        private const string ServicesDirectoryOption = "-sd";
        protected const string DtoProjectPathOption = "-dp";
        //private const string RepositoryDirectory = "Data\\Repositories";
        //private const string ServicesDirectory = "Services";
        public const string RepositoryClassType = "Repository";
        public const string RepositoriesFolder = "Repositories";
        private static readonly CSharpCodeProvider CSharpCodeProvider = new ();



        internal override string CommandName => "Gen";
        
        internal override (bool IsValid, string ErrorMessage) Validate()
        {
            ValidOptions.Add(RepositoryDirectoryOption);
            ValidOptions.Add(ServicesDirectoryOption);
            ValidOptions.Add(DtoProjectPathOption);
            
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
            const string servicesFolder = "Services";
            const string serviceClassType = "Service";
            const string dtoFolder = "Dto";
            const string dtoClassType = "Dto";
            const string cachedEntityFolder = "CacheModels";
            const string cacheEntityClassType = "CacheModel";

            Console.WriteLine("Running...");
            var persistenceProjectPath = $"{Environment.CurrentDirectory}";
            #if DEBUG
                persistenceProjectPath = "C:/Users/eawag/source/repos/solhigson-framework/src/Solhigson.Framework.Playground";
            #endif

            Console.WriteLine($"Using path: {persistenceProjectPath}");
            if (!Args.TryGetValue(DtoProjectPathOption, out var serviceProjectPath))
            {
                serviceProjectPath = persistenceProjectPath;
            }
            DtoProjectNamespace = new DirectoryInfo(serviceProjectPath).Name;

            var cachedEntityType = typeof(ICachedEntity);
            foreach (var entity in Models)
            {
                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, true, true); //generated interface
                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, true, false); //custom interface
                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, false,true); //generated class
                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, false,false); //custom class
                

                GenerateFile(serviceProjectPath, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false,
                    true, GetDtoProperties(entity, CSharpCodeProvider, false)); //generated dto
                
                GenerateFile(serviceProjectPath, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false,
                    false); //custom dto

                if (!cachedEntityType.IsAssignableFrom(entity))
                {
                    continue;
                }
                
                GenerateFile(persistenceProjectPath, cachedEntityFolder, cacheEntityClassType, entity.Name,
                    entity.Namespace, false, true, GetDtoProperties(entity, CSharpCodeProvider, true)); //generated dto
                    
                GenerateFile(persistenceProjectPath, cachedEntityFolder, cacheEntityClassType, entity.Name,
                    entity.Namespace, false, false); //custom dto
            }
            
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "Wrapper", RepositoryClassType, "", true, true, GetIRepositoryWrapperProperties(Models)); //generated interface
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "Wrapper", RepositoryClassType, "", true, false); //custom interface

            GenerateFile(persistenceProjectPath, RepositoriesFolder, "Wrapper", RepositoryClassType, "", false, true, GetRepositoryWrapperProperties(Models)); //generated class
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "Wrapper", RepositoryClassType, "", false, false); //custom class

            GenerateFile(persistenceProjectPath, RepositoriesFolder, "RepositoryBase", ApplicationName, "", true, true); // generated interface
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "RepositoryBase", ApplicationName, "", true, false); //custom interface
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "RepositoryBase", ApplicationName, "", false, true); //generated class
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "RepositoryBase", ApplicationName, "", false, false); //custom class

            Console.WriteLine("Completed");
        }

        private static string GetDtoProperties(Type entity, CSharpCodeProvider provider, bool getPropertiesWithCachedPropertyAttributeOnly)
        {
            var sBuilder = new StringBuilder();
            var properties = entity.GetProperties();
            if (getPropertiesWithCachedPropertyAttributeOnly)
            {
                var propertiesWithCachedPropertyAttribute = properties
                    .Where(t => t.GetAttribute<CachedPropertyAttribute>() != null)
                    .ToList();
                if (propertiesWithCachedPropertyAttribute.Any())
                {
                    properties = propertiesWithCachedPropertyAttribute.ToArray();
                }
            }

            foreach (var prop in properties)
            {
                sBuilder.AppendLine(GetPropertyDeclaration(prop, provider));
            }
            return sBuilder.ToString();
        }

        private static string GetPropertyDeclaration(PropertyInfo propertyInfo, CSharpCodeProvider provider)
        {
            var nullableIndicator = "";
            var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
            if (propertyType != null)
            {
                nullableIndicator = "?";
            }
            else
            {
                propertyType = propertyInfo.PropertyType;
            }
            var propertyTypeName = GetFriendlyName(propertyType, provider/**/);
            return "        public " + propertyTypeName + $"{nullableIndicator} " + propertyInfo.Name + " { get; set; }";
        }
        
        private string GetIRepositoryWrapperProperties(IList<Type> entities)
        {
            var sBuilder = new StringBuilder();

            foreach (var entity in entities)
            {
                var className = entity.Name + RepositoryClassType;
                sBuilder.AppendLine($"        {Namespace}.{RepositoriesFolder}.{AbstractionsFolderName}.I{className} {className}" + " { get; }");
            }

            return sBuilder.ToString();
        }
        
        private string GetRepositoryWrapperProperties(IList<Type> entities)
        {
            var sBuilder = new StringBuilder();

            foreach (var entity in entities)
            {
                var fieldName = "_" + entity.Name.ToCamelCase() + RepositoryClassType;
                var className = entity.Name + RepositoryClassType;
                sBuilder.AppendLine($"        private {Namespace}.{RepositoriesFolder}.{AbstractionsFolderName}.I{className} {fieldName};");
                sBuilder.AppendLine($"        public {Namespace}.{RepositoriesFolder}.{AbstractionsFolderName}.I{className} {className}");
                sBuilder.AppendLine("        { get { " + $"return {fieldName} ??= new {className}(DbContext);" + " } }");
                sBuilder.AppendLine("");
            }

            return sBuilder.ToString();
        }
        
        private static string GetFriendlyName(Type type, CSharpCodeProvider provider)
        {
            var friendlyName = type.Name;
            if (type.IsPrimitive || type == typeof(string))
            {
                return provider.GetTypeOutput(new CodeTypeReference(type));
            }
            if (type.IsGenericType)
            {
                var iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                var typeParameters = type.GetGenericArguments();
                for (var i = 0; i < typeParameters.Length; ++i)
                {
                    var typeParamName = GetFriendlyName(typeParameters[i], provider);
                    friendlyName += (i == 0 ? typeParamName : ", " + typeParamName);
                }
                friendlyName += ">";
            }
            friendlyName = $"{type.Namespace}.{friendlyName}";

            return friendlyName;
        }




    }
}