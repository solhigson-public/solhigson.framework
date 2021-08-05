using Solhigson.Framework.Data.Repository;
using [DbContextNamespace];
using [Namespace].[Folder].[AbstractionsFolder];
[EntityNameSpace]

namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class [Placeholder]Repository : [ApplicationName]RepositoryBase<[Placeholder]>, I[Placeholder]Repository
    {
        public [Placeholder]Repository([DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}