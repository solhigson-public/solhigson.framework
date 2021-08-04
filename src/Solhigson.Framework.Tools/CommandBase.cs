using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Solhigson.Framework.Data;

namespace Solhigson.Framework.Tools
{
    internal abstract class CommandBase
    {
        protected static List<string> ValidOptions = new() { AssemblyPathOption };
        protected const string AssemblyPathOption = "-a";

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
                var cachedEntityType = typeof(ICachedEntity);
                Models = assembly.GetTypes()
                    .Where(t => cachedEntityType.IsAssignableFrom(t) && !t.IsInterface).ToList();

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
    }
}