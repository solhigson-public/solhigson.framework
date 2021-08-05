
namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class [ApplicationName]RepositoryBase<T> : Solhigson.Framework.Data.Repository.RepositoryBase<T, [DbContextNamespace].[DbContextName]>, 
        [Namespace].[Folder].[AbstractionsFolder].I[ApplicationName]RepositoryBase<T> where T : class
    {
        public [ApplicationName]RepositoryBase([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }
    }
}