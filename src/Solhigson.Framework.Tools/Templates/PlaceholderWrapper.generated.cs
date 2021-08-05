
namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class RepositoryWrapper : [Namespace].[Folder].[AbstractionsFolder].IRepositoryWrapper
    {
        public [DbContextNamespace].[DbContextName] DbContext { get; }

[Properties]
        public RepositoryWrapper([DbContextNamespace].[DbContextName] dbContext)
        {
            DbContext = dbContext;
        }
        
        public System.Threading.Tasks.Task SaveChangesAsync()
        {
            return DbContext.SaveChangesAsync();
        }
                
        public int SaveChanges()
        {
            return DbContext.SaveChanges();
        }
    }
}