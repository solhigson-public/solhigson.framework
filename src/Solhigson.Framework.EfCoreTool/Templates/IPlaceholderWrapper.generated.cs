#nullable enable

namespace [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder]
{
    [GeneratedFileComment]
    public partial interface IRepositoryWrapper
    {
        [DbContextNamespace].[DbContextName] DbContext { get; }
        System.Threading.Tasks.Task SaveChangesAsync();
        int SaveChanges();
[Properties]
    }
}