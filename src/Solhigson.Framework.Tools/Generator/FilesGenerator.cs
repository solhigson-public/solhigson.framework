/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Solhigson.Framework.Tools.Generator
{
    internal abstract class FilesGenerator
    {
        protected IList<Type> Types { get; }
        protected string Path { get; }
        internal FilesGenerator(IList<Type> types, string path, string filePostFix, string folderName)
        {
            Types = types;
            Path = path;
            Postfix = filePostFix;
            FolderName = folderName;
        }

        internal abstract IEnumerable<ResourceDto> GetResources();
        
        internal string Postfix { get; }
        internal string FolderName { get; }

        void s()
        {
            foreach (var type in Types)
            {
                GenerateFile(type.Name, true, true);//generated interface
                GenerateFile(type.Name, true, false);//custom interface
                GenerateFile(type.Name, false, false);//custom concrete class
                GenerateFile(type.Name, false, false);//custom concrete generated class
            }
        }

        protected string GenerateFile(string entityName, bool isInterface, bool isGenerated)
        {
            var interfaceCustomPath = $"{Path}/{CommandBase.AbstractionsFolderName}/I{entityName}{Postfix}.cs";

            var interfaceIndicator = isInterface ? "I" : "";
            var abstractionsFolder = isInterface ? $"/{CommandBase.AbstractionsFolderName}" : "";
            var generatedIndicator = isGenerated ? ".generated" : "";
            
            var path = $"{Path}{abstractionsFolder}/I{entityName}{Postfix}.cs";
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"{CommandBase.ResourceNamePrefix}{interfaceIndicator}Placeholder{Postfix}{generatedIndicator}.cs");
            if (stream is null)
            {
                return null;
            }
            using var reader = new StreamReader(stream);
            var resource = reader.ReadToEnd().Replace("[Placeholder]", entityName);
            SaveFile(resource, path);
            
        }

        protected static void SaveFile(string file, string path)
        {
            if (!path.Contains(".generated.cs") && File.Exists(path))
            {
                return;
            }
            using var fileStream = File.Open(path, FileMode.OpenOrCreate);
            using var streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(file);
        }
        



    }
}
*/