using System.Threading.Tasks;
using [DbContextNamespace];
[EntityNameSpace]

namespace [Namespace].[Folder]
{
    //This file is ALWAYS overwritten, DO NOT place custom code here
    public partial interface IRepositoryWrapper
    {
        [DbContextName] DbContext { get; }
        Task SaveChangesAsync();
        int SaveChanges();
[Properties]
    }
}