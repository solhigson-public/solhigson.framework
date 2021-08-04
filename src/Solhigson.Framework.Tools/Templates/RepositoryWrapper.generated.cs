using System.Threading.Tasks;
using [DbContextNamespace];
[EntityNameSpace]

namespace [Namespace].[Folder]
{
    public partial class RepositoryWrapper : IRepositoryWrapper
    {
        public [DbContextName] DbContext { get; }

[Properties]
        public RepositoryWrapper([DbContextName] dbContext)
        {
            DbContext = dbContext;
        }
        
        public Task SaveChangesAsync()
        {
            return DbContext.SaveChangesAsync();
        }
                
        public int SaveChanges()
        {
            return DbContext.SaveChanges();
        }
    }
}