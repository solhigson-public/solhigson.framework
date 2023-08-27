using System;
using Autofac;

namespace [PersistenceProjectRootNamespace].[Folder]
{
    [GeneratedFileComment]
    public static partial class RepositoryDependencyRegister
    {
        public static void RegisterDependencies(Autofac.ContainerBuilder builder)
        {
            [RepositoryDependencies]

            RegisterDependenciesCustom(builder);
        }
    }
}