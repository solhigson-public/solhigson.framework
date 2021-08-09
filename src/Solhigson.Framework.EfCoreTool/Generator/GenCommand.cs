using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Attributes;
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
        private Type CachedEntityType = typeof(ICachedEntity);




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
            const string dtoFolder = "Dto";
            const string dtoClassType = "Dto";

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

            foreach (var entity in Models)
            {
                var isCached = CachedEntityType.IsAssignableFrom(entity);

                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name,
                    entity.Namespace, true, true, GetRepositoryMethods(entity, isCached, true), isCached); //generated interface
                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name,
                    entity.Namespace, true, false, isCachedEntity: isCached); //custom interface

                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name,
                    entity.Namespace, false, true,  GetRepositoryMethods(entity, isCached, false), isCached); //generated class
                GenerateFile(persistenceProjectPath, RepositoriesFolder, RepositoryClassType, entity.Name,
                    entity.Namespace, false, false, isCachedEntity: isCached); //custom class


                GenerateFile(serviceProjectPath, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false,
                    true, GetDtoProperties(entity, CSharpCodeProvider, false)); //generated dto

                GenerateFile(serviceProjectPath, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false,
                    false); //custom dto

                if (!isCached)
                {
                    continue;
                }

                GenerateFile(persistenceProjectPath, CachedEntityFolder, CacheEntityClassType, entity.Name,
                    entity.Namespace, false, true, GetDtoProperties(entity, CSharpCodeProvider, true)); //generated dto

                GenerateFile(persistenceProjectPath, CachedEntityFolder, CacheEntityClassType, entity.Name,
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

            GenerateFile(persistenceProjectPath, RepositoriesFolder, "CachedRepositoryBase", ApplicationName, "", true, true); // generated interface
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "CachedRepositoryBase", ApplicationName, "", true, false); //custom interface
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "CachedRepositoryBase", ApplicationName, "", false, true); //generated class
            GenerateFile(persistenceProjectPath, RepositoriesFolder, "CachedRepositoryBase", ApplicationName, "", false, false); //custom class
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
                if (!propertiesWithCachedPropertyAttribute.Any())
                {
                    return null;
                }
                properties = propertiesWithCachedPropertyAttribute.ToArray();
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
            return $"{GetTabSpace(2)}public " + propertyTypeName + $"{nullableIndicator} " + propertyInfo.Name + " { get; set; }";
        }
        
        private string GetIRepositoryWrapperProperties(IList<Type> entities)
        {
            var sBuilder = new StringBuilder();

            foreach (var entity in entities)
            {
                var className = entity.Name + RepositoryClassType;
                sBuilder.AppendLine($"{GetTabSpace(2)}{Namespace}.{RepositoriesFolder}.{AbstractionsFolderName}.I{className} {className}" + " { get; }");
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
                sBuilder.AppendLine($"{GetTabSpace(2)}private {Namespace}.{RepositoriesFolder}.{AbstractionsFolderName}.I{className} {fieldName};");
                sBuilder.AppendLine($"{GetTabSpace(2)}public {Namespace}.{RepositoriesFolder}.{AbstractionsFolderName}.I{className} {className}");
                sBuilder.AppendLine(GetTabSpace(2) + "{ get { " + $"return {fieldName} ??= new {className}(DbContext);" + " } }");
                sBuilder.AppendLine();
            }

            return sBuilder.ToString();
        }

        private string GetRepositoryMethods(Type type, bool isCacheEntity, bool isInterface)
        {
            var attributes = type.GetCustomAttributes<IndexAttribute>().ToList();
            var keyProp = type.GetProperties()
                .FirstOrDefault(t => t.HasAttribute<KeyAttribute>());
            
            if (keyProp != null)// && !attributes.Any(t => t.PropertyNames.Contains(keyProp.Name)))
            {
                var existing = attributes.FirstOrDefault(t => t.PropertyNames.Contains(keyProp.Name));
                if (existing != null)
                {
                    attributes.Remove(existing);
                }
                attributes.Insert(0, new IndexAttribute(keyProp.Name)
                {
                    IsUnique = true,
                });
            }
            if (!attributes.Any())
            {
                return string.Empty;
            }
            
            var className = $"{type.Namespace}.{type.Name}";
            var cachedSuffix = string.Empty;

            var sBuilder = new StringBuilder();
            foreach (var indexAttr in attributes)
            {
                sBuilder.AppendLine(GetMethodDefinition(indexAttr, type, className, cachedSuffix, isInterface));
                if (!isInterface)
                {
                    sBuilder.AppendLine(GenerateMethodBody(indexAttr, type, false));
                }
            }

            if (!isCacheEntity)
            {
                return sBuilder.ToString();
            }

            sBuilder.AppendLine();
            sBuilder.AppendLine($"{GetTabSpace(2)}//Cached Methods");
            className = $"{Namespace}.{CachedEntityFolder}.{type.Name}{CacheEntityClassType}";
            cachedSuffix = "Cached";
            foreach (var indexAttr in attributes)
            {
                sBuilder.AppendLine(GetMethodDefinition(indexAttr, type, className, cachedSuffix, isInterface));
                if (!isInterface)
                {
                    sBuilder.AppendLine(GenerateMethodBody(indexAttr, type, true));
                }
            }
            return sBuilder.ToString();

        }

        public static string GenerateMethodBody(IndexAttribute indexAttr, Type type, bool isCacheEntity)
        {
            var sBuilder = new StringBuilder();
            var propertyNames = new List<string> { indexAttr.PropertyNames[0] };

               
            if (indexAttr.PropertyNames.Count > 1)
            {
                for (var i = 1; i < indexAttr.PropertyNames.Count; i++)
                {
                    propertyNames.Add(indexAttr.PropertyNames[1]);
                }
            }

            var getMethod = "GetByCondition";
            var resultProjection = indexAttr.IsUnique
                ? ".FirstOrDefaultAsync()"
                : ".ToListAsync()";

            var awaitWord = "";
            if (isCacheEntity)
            {
                resultProjection = "";
                getMethod = indexAttr.IsUnique
                    ? "GetSingleCached"
                    : "GetListCached";
            }
            else
            {
                awaitWord = "await ";
            }
            
            
            sBuilder.AppendLine(GetTabSpace(2) + "{");
            sBuilder.AppendLine($"{GetTabSpace(3)}Expression<Func<{type.FullName}, bool>> query = ");
            sBuilder.Append($"{GetTabSpace(4)}t => t.{propertyNames[0]} == {propertyNames[0].ToCamelCase()}");
            if (propertyNames.Count > 0)
            {
                for (var i = 1; i < propertyNames.Count; i++)
                {
                    sBuilder.Append($"\n{GetTabSpace(4)}&& t.{propertyNames[i]} == {propertyNames[i].ToCamelCase()}");
                }
            }
            sBuilder.Append(';');
            sBuilder.AppendLine();
            sBuilder.AppendLine($"{GetTabSpace(3)}return {awaitWord}{getMethod}(query){resultProjection};");
            sBuilder.AppendLine(GetTabSpace(2) + "}");

            return sBuilder.ToString();
        }

        private static string GetMethodDefinition(IndexAttribute indexAttr, Type type, string className,
            string cachedSuffix,
            bool isInterface)
        {
            var propertyName = indexAttr.PropertyNames[0];
            var qualifier = isInterface ? "" : "public ";

            var propNameType = type.GetProperties().FirstOrDefault(t => t.Name == indexAttr.PropertyNames[0]);
            var parameters =
                $"{GetFriendlyName(propNameType.PropertyType, CSharpCodeProvider)} {propNameType.Name.ToCamelCase()}";

            if (indexAttr.PropertyNames.Count > 1)
            {
                for (var i = 1; i < indexAttr.PropertyNames.Count; i++)
                {
                    propertyName += $"And{indexAttr.PropertyNames[1]}";
                    propNameType = type.GetProperties().FirstOrDefault(t => t.Name == indexAttr.PropertyNames[i]);
                    parameters +=
                        $", {GetFriendlyName(propNameType.PropertyType, CSharpCodeProvider)} {propNameType.Name.ToCamelCase()}";
                }

            }

            if (!indexAttr.IsUnique)
            {
                className = $"System.Collections.Generic.IList<{className}>";
            }

            var asyncPostfix = "";

            if (string.IsNullOrWhiteSpace(cachedSuffix))
            {
                asyncPostfix = "Async";
                className = $"Task<{className}>";
                if (!isInterface)
                {
                    className = $"async {className}";
                }
            }

            return $"{GetTabSpace(2)}{qualifier}{className} GetBy{propertyName}{cachedSuffix}{asyncPostfix}({parameters})" + (isInterface ? ";" : "");
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

        private static string GetTabSpace(short noOfTabs)
        {
            var result = "";
            for (var i = 0; i < noOfTabs; i++)
            {
                result += "\t";
            }
            return result;
        }




    }
}