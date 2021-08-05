using Solhigson.Framework.Data.Repository;
using [DbContextNamespace];
using [Namespace].[Folder].[AbstractionsFolder];
[EntityNameSpace]

namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class [ApplicationName]RepositoryBase<T> : RepositoryBase<T, [DbContextName]>, I[ApplicationName]RepositoryBase<T> where T : class
    {
        public [ApplicationName]RepositoryBase([DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}