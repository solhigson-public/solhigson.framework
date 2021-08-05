using System.Threading.Tasks;
using [DbContextNamespace];
[EntityNameSpace]

namespace [Namespace].[Folder].[AbstractionsFolder]
{
    //[GeneratedFileComment]
    public partial interface IRepositoryWrapper
    {
        [DbContextName] DbContext { get; }
        Task SaveChangesAsync();
        int SaveChanges();
[Properties]
    }
}