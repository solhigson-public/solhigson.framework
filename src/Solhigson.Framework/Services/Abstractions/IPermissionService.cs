using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Services.Abstractions
{
    public interface IPermissionService : IServiceBase
    {
        ResponseInfo VerifyPermission(string permissionName, ClaimsPrincipal claimsPrincipal);
        Task<IList<PermissionDto>> GetAllPermissionsAsync();
        Task<IList<PermissionDto>> GetAllPermissionsForRoleAsync(string roleId);
        IList<PermissionDto> GetAllPermissionsForRoleCached(string roleId);
        Task<ResponseInfo<int>> DiscoverNewPermissions(Assembly controllerAssembly);
    }
}