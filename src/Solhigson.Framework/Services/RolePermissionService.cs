using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Services.Abstractions;

namespace Solhigson.Framework.Services
{
    public class RolePermissionService : ServiceBase, IRolePermissionService
    {
        public RolePermissionService(IRepositoryWrapper repositoryWrapper) : base(repositoryWrapper)
        {
        }
    }
}