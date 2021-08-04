using Solhigson.Framework.Data.Repository;
using [DbContextNamespace];
[EntityNameSpace]


namespace [Namespace].[Folder]
{
    //This file is ALWAYS overwritten, DO NOT place custom code here
    public partial class [Placeholder]Repository : RepositoryBase<[Placeholder], [DbContextName]>, I[Placeholder]Repository
    {
        public [Placeholder]Repository([DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}