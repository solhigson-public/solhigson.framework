using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Identity
{
    public class PermissionManager<TUser, TRole, TContext, TKey> 
        where TUser : SolhigsonUser<TKey> 
        where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey>
        where TRole : SolhigsonAspNetRole<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly TContext _dbContext;
        public IActionDescriptorCollectionProvider ActionDescriptorCollectionProvider { get; set; }
        public PermissionManager(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ResponseInfo VerifyPermission(string permissionName, ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal?.Identity?.IsAuthenticated == false)
            {
                return ResponseInfo.FailedResult("User not authenticated.");
            }

            var permission = _dbContext.Permissions.Where(t => t.Name == permissionName)
                .FromCacheSingle();
            if (permission is null)
            {
                return ResponseInfo.FailedResult("Resource not configured.");
            }

            var roles = claimsPrincipal?.FindAll(ClaimTypes.Role)
                .Where(t => !string.IsNullOrWhiteSpace(t.Value)).Select(t => t.Value).ToList();
            
            if (roles == null || !roles.Any())
            {
                return ResponseInfo.FailedResult("User not assigned any roles in Jwt Token.");
            }

            var roleIds = (from role in roles select 
                    _dbContext.Roles.Where(t => t.Name == role).FromCacheSingle()
                into roleObj where roleObj != null 
                select roleObj.Id).ToList();

            return roleIds.Any(roleId => _dbContext.RolePermissions.Where(t => t.RoleId.Equals(roleId) && t.PermissionId == permission.Id)
                .FromCacheSingle() is not null) 
                ? ResponseInfo.SuccessResult() 
                : ResponseInfo.FailedResult();
        }

        public async Task AddPermission(SolhigsonPermission permission)
        {
            _dbContext.Permissions.Add(permission);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddRolePermission(SolhigsonRolePermission<TKey> rolePermission)
        {
            _dbContext.RolePermissions.Add(rolePermission);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRolePermission(SolhigsonRolePermission<TKey> rolePermission)
        {
            _dbContext.RolePermissions.Remove(rolePermission);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdatePermission(SolhigsonPermission permission)
        {
            _dbContext.Permissions.Update(permission);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IList<SolhigsonPermission>> GetAllPermissionsAsync()
        {
            return await _dbContext.Permissions.ToListAsync();
        }

        public async Task<IList<SolhigsonPermission>> GetAllPermissionsForRoleAsync(string roleName)
        {
            return await (from p in _dbContext.Permissions
                join rp in _dbContext.RolePermissions
                    on p.Id equals rp.PermissionId
                    join ar in _dbContext.Roles
                    on rp.RoleId equals ar.Id
                where ar.Name == roleName
                select p).ToListAsync();
        }
        
        public async Task<IList<string>> GetAllowedRolesForPermissionAsync(string permissionName)
        {
            return await (from ar in _dbContext.Roles
                join rp in _dbContext.RolePermissions
                    on ar.Id equals rp.RoleId
                    join p in _dbContext.Permissions
                    on rp.PermissionId equals p.Id
                where p.Name == permissionName
                select ar.Name).ToListAsync();
        }
        
        public IList<SolhigsonPermission> GetAllPermissionsForRoleCached(string roleName)
        {
            return (from rolePerm in _dbContext.RolePermissions
                join role in _dbContext.Roles
                    on rolePerm.RoleId equals role.Id
                join perm in _dbContext.Permissions
                    on rolePerm.PermissionId equals perm.Id
                select perm).FromCacheList(typeof(SolhigsonRolePermission<TKey>));
        }

        public async Task<ResponseInfo<int>> DiscoverNewPermissions(Assembly controllerAssembly)
        {
            var response = new ResponseInfo<int>();
            if (controllerAssembly is null)
            {
                return response.Fail("Controller assembly is null");
            }
            var controllerTypes = from type in controllerAssembly.GetTypes() where type.IsSubclassOf(typeof(ControllerBase)) select type;
            var count = 0;
            foreach (var controllerType in controllerTypes)
            {
                var methodInfos = controllerType.GetMethods()
                    .Where(t => t.ReturnType != typeof(void) && t.DeclaringType == controllerType && t.IsPublic);
                foreach (var methodInfo in methodInfos)
                {
                    var permissionAttribute = methodInfo.GetAttribute<PermissionAttribute>(false);
                    if (permissionAttribute is null)
                    {
                        continue;
                    }

                    if (_dbContext.Permissions.Any(t =>
                        t.Name == permissionAttribute.Name))
                    {
                        continue;
                    }
                    
                    var actionInfo = ActionDescriptorCollectionProvider.ActionDescriptors.Items.FirstOrDefault(x => x is ControllerActionDescriptor controllerActionDescriptor
                        && controllerActionDescriptor.ControllerTypeInfo.AsType() == controllerType
                        && controllerActionDescriptor.ActionName == methodInfo.Name);

                    var permission = new SolhigsonPermission();
                    permission = permissionAttribute.Adapt(permission);
                    _dbContext.Permissions.Add(permission);
                    permission.Url = actionInfo?.AttributeRouteInfo?.Template;
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                        count++;
                        this.ELogInfo($"Discovered permission protected endpoint: [{permission.Name}] - [{permission.Url}]");
                    }
                    catch (Exception e)
                    {
                        this.ELogError(e, "Saving permission protected endpoint", permission);
                    }
                }
            }
            return response.Success(count);
        }


    }
}