using Microsoft.Extensions.DependencyInjection;

namespace [PersistenceProjectRootNamespace].[Folder]
{
    [GeneratedFileComment]
    public partial class RepositoryWrapper : [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder].IRepositoryWrapper
    {
        public [DbContextNamespace].[DbContextName] DbContext { get; }
        public System.IServiceProvider ServiceProvider { get; }

[Properties]
        public RepositoryWrapper([DbContextNamespace].[DbContextName] dbContext, System.IServiceProvider serviceProvider)
        {
            DbContext = dbContext;
            ServiceProvider = serviceProvider;
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