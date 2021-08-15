
namespace [DtoProjectNamespace].[Folder]
{
    [GeneratedFileComment]
    public abstract partial class ServiceBase
    {
        protected [PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper RepositoryWrapper { get; }

        public ServiceBase([PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper repositoryWrapper)
        {
            RepositoryWrapper = repositoryWrapper;
        }
    }
}