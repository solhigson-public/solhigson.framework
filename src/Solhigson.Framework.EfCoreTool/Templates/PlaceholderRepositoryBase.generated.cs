
namespace [PersistenceProjectRootNamespace].[Folder]
{
    [GeneratedFileComment]
    public partial class [ApplicationNameClassSafe]RepositoryBase<T> : Solhigson.Framework.Data.Repository.RepositoryBase<T, [DbContextNamespace].[DbContextName]>, 
        [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder].I[ApplicationNameClassSafe]RepositoryBase<T> where T : class, new()
    {
        public [ApplicationNameClassSafe]RepositoryBase([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}