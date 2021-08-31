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
        Task<IList<PermissionDto>> GetAllPermissionsForRoleAsync(string roleName);
        IList<PermissionDto> GetAllPermissionsForRoleCached(string roleName);
        Task<ResponseInfo<int>> DiscoverNewPermissions(Assembly controllerAssembly);
        Task AddPermission(PermissionDto permissionDto);
        Task AddRolePermission(RolePermissionDto permissionDto);
        Task RemoveRolePermission(RolePermissionDto permissionDto);
        Task UpdatePermission(PermissionDto permissionDto);
        Task<IList<string>> GetAllowedRolesForPermissionAsync(string permissionName);
    }
}