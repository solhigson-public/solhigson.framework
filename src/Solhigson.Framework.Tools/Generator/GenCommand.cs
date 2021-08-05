using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Tools.Generator
{
    internal class GenCommand : CommandBase
    {
        private const string RepositoryDirectoryOption = "-rd";
        private const string ServicesDirectoryOption = "-sd";
        private const string RepositoryDirectory = "Data\\Repositories";
        private const string ServicesDirectory = "Services";
        public const string RepositoryClassType = "Repository";
        public const string RepositoriesFolder = "Repositories";



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
            const string servicesFolder = "Services";
            const string serviceClassType = "Service";
            const string dtoFolder = "Dto";
            const string dtoClassType = "Dto";

            Console.WriteLine("Running...");
            //var path = $"{Environment.CurrentDirectory}";
            var path = "C:/Users/eawag/source/repos/solhigson-framework/src/Solhigson.Framework.Playground";

            foreach (var entity in Models)
            {
                GenerateFile(path, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, true, true); //generated interface
                GenerateFile(path, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, true, false); //custom interface
                GenerateFile(path, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, false,true); //generated class
                GenerateFile(path, RepositoriesFolder, RepositoryClassType, entity.Name, entity.Namespace, false,false); //custom class
                

                GenerateFile(path, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false, true, GetDtoProperties(entity)); //generated dto
                GenerateFile(path, dtoFolder, dtoClassType, entity.Name, entity.Namespace, false, false); //custom dto
                
            }
            
            GenerateFile(path, RepositoriesFolder, "Wrapper", RepositoryClassType, "", true, true, GetIRepositoryWrapperProperties(Models)); //generated interface
            GenerateFile(path, RepositoriesFolder, "Wrapper", RepositoryClassType, "", true, false); //custom interface

            GenerateFile(path, RepositoriesFolder, "Wrapper", RepositoryClassType, "", false, true, GetRepositoryWrapperProperties(Models)); //generated class
            GenerateFile(path, RepositoriesFolder, "Wrapper", RepositoryClassType, "", false, false); //custom class

            GenerateFile(path, RepositoriesFolder, "RepositoryBase", ApplicationName, "", true, true); // generated interface
            GenerateFile(path, RepositoriesFolder, "RepositoryBase", ApplicationName, "", true, false); //custom interface
            GenerateFile(path, RepositoriesFolder, "RepositoryBase", ApplicationName, "", false, true); //generated class
            GenerateFile(path, RepositoriesFolder, "RepositoryBase", ApplicationName, "", false, false); //custom class

            Console.WriteLine("Completed");
        }

        private static string GetDtoProperties(Type entity)
        {
            var provider = new CSharpCodeProvider();
            var sBuilder = new StringBuilder();

            foreach (var prop in entity.GetProperties())
            {
                string propertyName;
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                {
                    propertyName = provider.GetTypeOutput(new CodeTypeReference(prop.PropertyType));
                }
                else
                {
                    propertyName = prop.PropertyType.Name;
                }
                sBuilder.AppendLine("        public " + propertyName + " " + prop.Name + " { get; set; }");
            }

            return sBuilder.ToString();
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
        





    }
}