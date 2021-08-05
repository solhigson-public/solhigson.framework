using System.Threading.Tasks;
using [DbContextNamespace];
using [Namespace].[Folder].[AbstractionsFolder];
[EntityNameSpace]

namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
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