
namespace [PersistenceProjectRootNamespace].[Folder]
{
    [GeneratedFileComment]
    public partial class [ApplicationNameClassSafe]CachedRepositoryBase<T, TCacheModel> 
        : Solhigson.Framework.Data.Repository.CachedRepositoryBase<T, [DbContextNamespace].[DbContextName], TCacheModel> 
        where T : class, new() where TCacheModel : class
    {
        public [ApplicationNameClassSafe]CachedRepositoryBase([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}