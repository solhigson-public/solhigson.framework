
namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class [ApplicationName]CachedRepositoryBase<T, TCacheModel> 
        : Solhigson.Framework.Data.Repository.CachedRepositoryBase<T, [DbContextNamespace].[DbContextName], TCacheModel> 
        where T : class, new() where TCacheModel : class
    {
        public [ApplicationName]CachedRepositoryBase([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}