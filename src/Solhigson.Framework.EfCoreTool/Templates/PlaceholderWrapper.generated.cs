#nullable enable
using System.Threading;

namespace [PersistenceProjectRootNamespace].[Folder]
{
    [GeneratedFileComment]
    public partial class RepositoryWrapper : [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder].IRepositoryWrapper
    {
        public [DbContextNamespace].[DbContextName] DbContext { get; }

[Properties]
        public RepositoryWrapper([DbContextNamespace].[DbContextName] dbContext)
        {
            DbContext = dbContext;
        }
        
        public System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return DbContext.SaveChangesAsync();
        }
                
        public int SaveChanges()
        {
            return DbContext.SaveChanges();
        }
    }
}