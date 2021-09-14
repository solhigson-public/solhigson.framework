﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.EfCoreTool.Generator
{
    internal class GenCommand : CommandBase
    {
        private const string PersistenceProjectPathOption = "-pp";
        private const string RepositoryDirectoryOption = "-rd";
        private const string ServicesDirectoryOption = "-sd";
        private const string DtoProjectPathOption = "-cp";
        private const string TestsProjectPathOption = "-tp";
        private const string RepositoryClassType = "Repository";
        private static readonly CSharpCodeProvider CSharpCodeProvider = new ();




        internal override string CommandName => "Gen";

        protected override (bool IsValid, string ErrorMessage) Validate()
        {
            ValidOptions.Add(PersistenceProjectPathOption);
            ValidOptions.Add(RepositoryDirectoryOption);
            ValidOptions.Add(ServicesDirectoryOption);
            ValidOptions.Add(DtoProjectPathOption);
            ValidOptions.Add(TestsProjectPathOption);
            ValidOptions.Add(RootNamespaceOption);
            
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
            ServicesFolder = "Services";
            const string dtoClassType = "Dto";

            Console.WriteLine("Running...");
            
            if (!Args.TryGetValue(PersistenceProjectPathOption, out var persistenceProjectPath))
            {
                persistenceProjectPath = Environment.CurrentDirectory;
            }
            #if DEBUG
                persistenceProjectPath = "C:/Users/eawag/source/repos/solhigson-framework/src/Solhigson.Framework.Playground";
            #endif


            Console.WriteLine($"Using path: {persistenceProjectPath}");
            if (!Args.TryGetValue(DtoProjectPathOption, out var serviceProjectPath))
            {
                serviceProjectPath = persistenceProjectPath;
            }

            Args.TryGetValue(TestsProjectPathOption, out var testsProjectPath);
            Args.TryGetValue(RepositoryDirectoryOption, out var repositoryDirectoryPath);
            //RepositoriesFolder = "Repositories";
            if (!string.IsNullOrWhiteSpace(repositoryDirectoryPath))
            {
                //persistenceProjectPath += $"/{repositoryDirectoryPath}";
                RepositoryNamespace = $"{repositoryDirectoryPath}.{RepositoriesFolder}";
                CachedEntityNamespace = $"{repositoryDirectoryPath}.{CachedEntityFolder}";
            }
            else
            {
                RepositoryNamespace = RepositoriesFolder;
                CachedEntityNamespace = CachedEntityFolder;
            }


            DtoProjectNamespace = new DirectoryInfo(serviceProjectPath).Name;

            foreach (var entity in Models)
            {
                if (entity == null)
                {
                    continue;
                }
                var isCached = typeof(ICachedEntity).IsAssignableFrom(entity)
                    || entity.GetInterface("Solhigson.Framework.Data.Caching.ICachedEntity") != null;

                GenerateFile(persistenceProjectPath, RepositoryNamespace, RepositoryClassType, entity.Name,
                    entity.Namespace, true, true, GetRepositoryMethods(entity, isCached, true), isCached); //generated interface
                GenerateFile(persistenceProjectPath, RepositoryNamespace, RepositoryClassType, entity.Name,
                    entity.Namespace, true, false, isCachedEntity: isCached); //custom interface

                GenerateFile(persistenceProjectPath, RepositoryNamespace, RepositoryClassType, entity.Name,
                    entity.Namespace, false, true,  GetRepositoryMethods(entity, isCached, false), isCached); //generated class
                GenerateFile(persistenceProjectPath, RepositoryNamespace, RepositoryClassType, entity.Name,
                    entity.Namespace, false, false, isCachedEntity: isCached); //custom class


                GenerateFile(serviceProjectPath, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false,
                    true, GetDtoProperties(entity, false)); //generated dto

                GenerateFile(serviceProjectPath, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false,
                    false); //custom dto

                if (!isCached)
                {
                    continue;
                }

                GenerateFile(persistenceProjectPath, CachedEntityNamespace, CacheEntityClassType, entity.Name,
                    entity.Namespace, false, true, GetDtoProperties(entity, true)); //generated cached dto

                GenerateFile(persistenceProjectPath, CachedEntityNamespace, CacheEntityClassType, entity.Name,
                    entity.Namespace, false, false); //custom cached dto
            }
            
            GenerateFile(serviceProjectPath, ServicesFolder, "ServiceBase", "", "Service", true,
                true); //generated IServiceBase

            GenerateFile(serviceProjectPath, ServicesFolder, "ServiceBase", "", "Service", false,
                true); //generated ServiceBase

            GenerateFile(serviceProjectPath, ServicesFolder, "ServiceBase", "", "Service", false,
                false); //Custom ServiceBase

            if (!string.IsNullOrWhiteSpace(testsProjectPath))
            {
                GenerateFile(testsProjectPath, "", "BaseTest", "", "", false,
                    true); //generated dto
            }


            GenerateFile(persistenceProjectPath, RepositoryNamespace, "Wrapper", RepositoryClassType, "", true, true, GetIRepositoryWrapperProperties(Models)); //generated interface
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "Wrapper", RepositoryClassType, "", true, false); //custom interface

            GenerateFile(persistenceProjectPath, RepositoryNamespace, "Wrapper", RepositoryClassType, "", false, true, GetRepositoryWrapperProperties(Models)); //generated class
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "Wrapper", RepositoryClassType, "", false, false); //custom class

            GenerateFile(persistenceProjectPath, RepositoryNamespace, "RepositoryBase", ApplicationName, "", true, true); // generated interface
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "RepositoryBase", ApplicationName, "", true, false); //custom interface
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "RepositoryBase", ApplicationName, "", false, true); //generated class
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "RepositoryBase", ApplicationName, "", false, false); //custom class

            GenerateFile(persistenceProjectPath, RepositoryNamespace, "CachedRepositoryBase", ApplicationName, "", true, true); // generated interface
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "CachedRepositoryBase", ApplicationName, "", true, false); //custom interface
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "CachedRepositoryBase", ApplicationName, "", false, true); //generated class
            GenerateFile(persistenceProjectPath, RepositoryNamespace, "CachedRepositoryBase", ApplicationName, "", false, false); //custom class
            Console.WriteLine("Completed");
        }

        private static string GetDtoProperties(Type entity, bool getPropertiesWithCachedPropertyAttributeOnly)
        {
            var sBuilder = new StringBuilder();
            var properties = entity.GetProperties();
            if (getPropertiesWithCachedPropertyAttributeOnly)
            {
                var propertiesWithCachedPropertyAttribute = properties
                    .Where(t => t.GetAttribute<CachedPropertyAttribute>() != null
                    || t.GetCustomAttributes().Any(s => $"{s.TypeId}" == "Solhigson.Framework.Data.Attributes.CachedPropertyAttribute"))
                    .ToList();
                if (!propertiesWithCachedPropertyAttribute.Any())
                {
                    return null;
                }
                properties = propertiesWithCachedPropertyAttribute.ToArray();
            }

            foreach (var prop in properties)
            {
                var type = prop.PropertyType;
                if (!IsSystemType(type))
                {
                    continue;
                }
                sBuilder.AppendLine(GetPropertyDeclaration(prop));
            }
            return sBuilder.ToString();
        }

        private static bool IsSystemType(Type type)
        {
            while (true)
            {
                if (type == null)
                {
                    return false;
                }

                if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)
                || type == typeof(DateTime))
                {
                    return true;
                }

                var nullable = Nullable.GetUnderlyingType(type);
                type = nullable;
            }
        }

        private static string GetPropertyDeclaration(PropertyInfo propertyInfo)
        {
            var propertyTypeName = GetTypeName(propertyInfo.PropertyType);
            return $"{GetTabSpace(2)}public " + propertyTypeName + " " + propertyInfo.Name + " { get; set; }";
        }

        private static string GetTypeName(Type type)
        {
            var nullableIndicator = "";
            var propertyType = Nullable.GetUnderlyingType(type);
            if (propertyType != null)
            {
                nullableIndicator = "?";
            }
            else
            {
                propertyType = type;
            }
            return GetFriendlyName(propertyType) + nullableIndicator;
        }
        
        private string GetIRepositoryWrapperProperties(IList<Type> entities)
        {
            var sBuilder = new StringBuilder();

            foreach (var entity in entities)
            {
                var className = entity.Name + RepositoryClassType;
                sBuilder.AppendLine($"{GetTabSpace(2)}{PersistenceProjectRootNamespace}.{RepositoryNamespace}.{AbstractionsFolderName}.I{className} {className}" + " { get; }");
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
                sBuilder.AppendLine($"{GetTabSpace(2)}private {PersistenceProjectRootNamespace}.{RepositoryNamespace}.{AbstractionsFolderName}.I{className} {fieldName};");
                sBuilder.AppendLine($"{GetTabSpace(2)}public {PersistenceProjectRootNamespace}.{RepositoryNamespace}.{AbstractionsFolderName}.I{className} {className}");
                sBuilder.AppendLine(GetTabSpace(2) + "{ get { " + $"return {fieldName} ??= new {PersistenceProjectRootNamespace}.{RepositoryNamespace}.{className}(DbContext);" + " } }");
                sBuilder.AppendLine();
            }

            return sBuilder.ToString();
        }

        private static bool Same(IReadOnlyList<string> first, IReadOnlyList<string> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (var i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        private string GetRepositoryMethods(Type type, bool isCacheEntity, bool isInterface)
        {
            var attributes = type.GetCustomAttributes<IndexAttribute>().ToList();
            var keyProp = type.GetProperties()
                .FirstOrDefault(t => t.HasAttribute<KeyAttribute>());
            var distinctAttributes = new List<IndexAttribute>();
            foreach (var attr in attributes)
            {
                if (distinctAttributes.Any(t => Same(t.PropertyNames, attr.PropertyNames)))
                {
                    continue;
                }
                distinctAttributes.Add(attr);
            }

            attributes = distinctAttributes;

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
            className = GetCachedDtoClassType(type);
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

        private string GetCachedDtoClassType(Type type)
        {
            return $"{PersistenceProjectRootNamespace}.{CachedEntityNamespace}.{type.Name}{CacheEntityClassType}";
        }

        private string GenerateMethodBody(IndexAttribute indexAttr, Type type, bool isCacheEntity)
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

            var getMethod = "Get";
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

            var totalPropCount = propertyNames.Count;
            var totalNullableTypesCount = type.GetProperties()
                .Where(t => propertyNames.Contains(t.Name))
                .Where(t => (Nullable.GetUnderlyingType(t.PropertyType) != null || !t.PropertyType.IsPrimitive
                                                                               || t.PropertyType == typeof(string))
                                                                                && t.PropertyType != typeof(DateTime))
                .ToList();
            var nullCheck = "";
            if (totalNullableTypesCount.Any())
            {
                nullCheck = $"{GetTabSpace(3)}if ({totalNullableTypesCount[0].Name.ToCamelCase()} is null";
                if (totalNullableTypesCount.Count > 0)
                {
                    for (var i = 1; i < totalNullableTypesCount.Count; i++)
                    {
                        nullCheck += $" || {totalNullableTypesCount[i].Name.ToCamelCase()} is null";
                    }
                }

                var returnType = "null";
                if (!indexAttr.IsUnique)
                {
                    returnType = isCacheEntity
                        ? GetCachedDtoClassType(type)
                        : type.FullName;
                    returnType = $"new System.Collections.Generic.List<{returnType}>()";
                }
                
                nullCheck += ") { return " + returnType + "; }\n";
                
            }
            
            
            sBuilder.AppendLine(GetTabSpace(2) + "{");
            sBuilder.AppendLine(nullCheck);
            sBuilder.AppendLine($"{GetTabSpace(3)}Expression<Func<{type.FullName}, bool>> query = ");
            sBuilder.Append($"{GetTabSpace(4)}t => t.{propertyNames[0]} == {propertyNames[0].ToCamelCase()}");
            if (totalPropCount > 0)
            {
                for (var i = 1; i < totalPropCount; i++)
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
            var props = type.GetProperties();

            var propertyInfo = props.FirstOrDefault(t => t.Name == indexAttr.PropertyNames[0]);
            if (propertyInfo == null)
            {
                throw new Exception($"Index property: {indexAttr.PropertyNames[0]} not found in {type.Name}'s properties");
            }
            var parameters =
                $"{GetTypeName(propertyInfo.PropertyType)} {propertyInfo.Name.ToCamelCase()}";

            if (indexAttr.PropertyNames.Count > 1)
            {
                for (var i = 1; i < indexAttr.PropertyNames.Count; i++)
                {
                    propertyName += $"And{indexAttr.PropertyNames[1]}";
                    propertyInfo = props.FirstOrDefault(t => t.Name == indexAttr.PropertyNames[i]);
                    if (propertyInfo == null)
                    {
                        throw new Exception($"Index property: {indexAttr.PropertyNames[i]} not found in {type.Name}'s properties");
                    }
                    parameters +=
                        $", {GetTypeName(propertyInfo.PropertyType)} {propertyInfo.Name.ToCamelCase()}";
                }

            }

            if (!indexAttr.IsUnique)
            {
                className = $"System.Collections.Generic.List<{className}>";
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

        
        private static string GetFriendlyName(Type type)
        {
            var friendlyName = type.Name;
            if (type.IsPrimitive || type == typeof(string))
            {
                return CSharpCodeProvider.GetTypeOutput(new CodeTypeReference(type));
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
                    var typeParamName = GetFriendlyName(typeParameters[i]);
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