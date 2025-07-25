#nullable enable
using System.Threading;

namespace [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder]
{
    [GeneratedFileComment]
    public partial interface IRepositoryWrapper
    {
        [DbContextNamespace].[DbContextName] DbContext { get; }
        System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default);
        int SaveChanges();
[Properties]
    }
}