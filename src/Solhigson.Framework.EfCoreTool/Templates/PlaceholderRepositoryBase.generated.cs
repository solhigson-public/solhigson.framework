
namespace [PersistenceProjectRootNamespace].[Folder]
{
    [GeneratedFileComment]
    public partial class [ApplicationName]RepositoryBase<T> : Solhigson.Framework.Data.Repository.RepositoryBase<T, [DbContextNamespace].[DbContextName]>, 
        [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder].I[ApplicationName]RepositoryBase<T> where T : class, new()
    {
        public [ApplicationName]RepositoryBase([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}