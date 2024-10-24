﻿using System;
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
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities.Extensions;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Identity;

public class PermissionManager<TUser, TRole, TContext, TKey> 
    where TUser : SolhigsonUser<TKey, TRole> 
    where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey>
    where TRole : SolhigsonAspNetRole<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly TContext _dbContext;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    public PermissionManager(TContext dbContext, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    {
        _dbContext = dbContext;
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
    }

    public ResponseInfo VerifyPermission(string permissionName, ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Identity?.IsAuthenticated == false)
        {
            return ResponseInfo.FailedResult("User not authenticated.");
        }

        return VerifyPermission(permissionName, claimsPrincipal?.FindAll(ClaimTypes.Role)
            .Where(t => !string.IsNullOrWhiteSpace(t.Value)).Select(t => t.Value).ToList());

    }
        
    public ResponseInfo VerifyPermission(string permissionName, string role)
    {
        return VerifyPermission(permissionName, new [] { role});
    }

    public ResponseInfo VerifyPermission(string permissionName, IReadOnlyCollection<string> roles)
    {
        if (roles == null || !roles.Any())
        {
            return ResponseInfo.FailedResult("User not assigned any roles in Jwt Token.");
        }

        var query = (from p in _dbContext.Permissions
            join rp in _dbContext.RolePermissions
                on p.Id equals rp.PermissionId
            join r in _dbContext.Roles
                on rp.RoleId equals r.Id
            where roles.Contains(r.Name) && p.Name == permissionName
            select rp).FromCacheSingle(typeof(SolhigsonPermission),
            typeof(SolhigsonRolePermission<TKey>), typeof(TRole));

        return query is not null
            ? ResponseInfo.SuccessResult() 
            : ResponseInfo.FailedResult("User does not have the required permission");
       
    }
    
    public async Task AddPermissionAsync(SolhigsonPermission permission)
    {
        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ResponseInfo> GiveAccessToRoleAsync(string roleName, string permissionName)
    {
        var response = new ResponseInfo();
        var role = await _dbContext.Roles.FirstOrDefaultAsync(t => t.Name == roleName);
        if (role is null)
        {
            return response.Fail($"Role does not exist: {roleName}");
        }
        var permission = await _dbContext.Permissions.FirstOrDefaultAsync(t => t.Name == permissionName);
        if (permission is null)
        {
            return response.Fail($"Permission does not exist: {permissionName}");
        }

        var existing = await _dbContext.RolePermissions.FirstOrDefaultAsync(
            t => t.RoleId.Equals(role.Id) && t.PermissionId == permission.Id);
        if (existing is null)
        {
            await AddRolePermissionAsync(new SolhigsonRolePermission<TKey>
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }
        return response.Success();
    }
        
    public async Task<ResponseInfo> RemoveAccessFromRoleAsync(string roleName, string permissionName)
    {
        var response = new ResponseInfo();
        var role = await _dbContext.Roles.FirstOrDefaultAsync(t => t.Name == roleName);
        if (role is null)
        {
            return response.Fail($"Role does not exist: {roleName}");
        }
        var permission = await _dbContext.Permissions.FirstOrDefaultAsync(t => t.Name == permissionName);
        if (permission is null)
        {
            return response.Fail($"Permission does not exist: {permissionName}");
        }

        var existing = await _dbContext.RolePermissions.FirstOrDefaultAsync(
            t => t.RoleId.Equals(role.Id) && t.PermissionId == permission.Id);
        if (existing is not null)
        {
            await RemoveRolePermissionAsync(existing);
        }
        return response.Success();
    }

    public async Task AddRolePermissionsAsync(IEnumerable<SolhigsonRolePermission<TKey>> rolePermissions)
    {
        _dbContext.RolePermissions.AddRange(rolePermissions);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRolePermissionAsync(SolhigsonRolePermission<TKey> rolePermission)
    {
        _dbContext.RolePermissions.Add(rolePermission);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveRolePermissionAsync(SolhigsonRolePermission<TKey> rolePermission)
    {
        _dbContext.RolePermissions.Remove(rolePermission);
        await _dbContext.SaveChangesAsync();
    }
        
    public async Task RemoveRolePermissionsAsync(IEnumerable<SolhigsonRolePermission<TKey>> rolePermissions)
    {
        _dbContext.RolePermissions.RemoveRange(rolePermissions);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdatePermissionAsync(SolhigsonPermission permission)
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
            where role.Name == roleName
            select perm).FromCacheList(typeof(SolhigsonRolePermission<TKey>), typeof(TRole), typeof(SolhigsonPermission));
    }

    public IList<SolhigsonPermissionDto> GetMenuPermissionsForRoleCached(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Identity?.IsAuthenticated == false)
        {
            return new List<SolhigsonPermissionDto>();
        }

        var role = claimsPrincipal?.FindFirstValue(ClaimTypes.Role);
            
        return GetMenuPermissionsForRoleCached(role);
    }

    public IList<SolhigsonPermissionDto> GetMenuPermissionsForRoleCached(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return new List<SolhigsonPermissionDto>();
        }
            
        var query = from rolePerm in _dbContext.RolePermissions
            join role in _dbContext.Roles
                on rolePerm.RoleId equals role.Id
            join perm in _dbContext.Permissions
                on rolePerm.PermissionId equals perm.Id
            where perm.IsMenu && perm.IsMenuRoot && perm.Enabled && role.Name == roleName && string.IsNullOrWhiteSpace(perm.ParentId)
            select perm;

        var result = query.GetCustomResultFromCache<List<SolhigsonPermissionDto>, SolhigsonPermission>();
        if (result != null)
        {
            return result;
        }

        var topLevel = query.OrderBy(t => t.MenuIndex)
            .ThenBy(t => t.Name).AsNoTracking().ToList();

        var children = (from rolePerm in _dbContext.RolePermissions
            join role in _dbContext.Roles
                on rolePerm.RoleId equals role.Id
            join perm in _dbContext.Permissions
                on rolePerm.PermissionId equals perm.Id
            where perm.IsMenu && !perm.IsMenuRoot && perm.Enabled && role.Name == roleName && !string.IsNullOrWhiteSpace(perm.ParentId)
            select perm).OrderBy(t => t.MenuIndex).ThenBy(t => t.Name).AsNoTracking().ToList();
            
        foreach(var parent in topLevel)
        {
            parent.Children ??= new List<SolhigsonPermission>();
        }
            
        foreach (var child in children)
        {
            var parent = topLevel.FirstOrDefault(t => t.Id == child.ParentId);
            if (parent is null)
            {
                parent = _dbContext.Permissions.FirstOrDefault(t => t.Id == child.ParentId);
                if (parent is null)
                {
                    continue;
                }
                parent.Children ??= new List<SolhigsonPermission>();
                topLevel.Add(parent);
            }

            if (parent.Children.All(t => t.Name != child.Name))
            {
                parent.Children.Add(child);
            }
        }

        result = new List<SolhigsonPermissionDto>();
        foreach (var parent in topLevel.Where(parent => parent.Children?.Any() == true
                                                        || !string.IsNullOrWhiteSpace(parent.Url)
                                                        || !string.IsNullOrWhiteSpace(parent.OnClickFunction)))
        {
            var permissionDto = parent.Adapt<SolhigsonPermissionDto>();
            permissionDto.Children = new List<SolhigsonPermissionDto>();
            foreach (var child in parent.Children)
            {
                permissionDto.Children.Add(child.Adapt<SolhigsonPermissionDto>());
            }
            result.Add(permissionDto);
        }

        query.AddCustomResultToCache(result, typeof(SolhigsonRolePermission<TKey>), typeof(TRole), typeof(SolhigsonPermission));
        return result;
    }

    public async Task<ResponseInfo<int>> DiscoverNewPermissionsAsync(Assembly controllerAssembly,
        Dictionary<string, string> customPermissions = null)
    {
        var response = new ResponseInfo<int>();
        try
        {
            if (controllerAssembly is null)
            {
                return response.Fail("Controller assembly is null");
            }

            var controllerTypes = from type in controllerAssembly.GetTypes()
                where type.IsSubclassOf(typeof(ControllerBase))
                select type;
            var count = 0;
            var permissionList = new Dictionary<string, SolhigsonPermission>();
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

                    if (permissionList.ContainsKey(permissionAttribute.Name) || _dbContext.Permissions.Any(t =>
                            t.Name == permissionAttribute.Name))
                    {
                        continue;
                    }

                    var actionInfo = _actionDescriptorCollectionProvider.ActionDescriptors.Items.FirstOrDefault(x =>
                        x is ControllerActionDescriptor controllerActionDescriptor
                        && controllerActionDescriptor.ControllerTypeInfo.AsType() == controllerType
                        && controllerActionDescriptor.ActionName == methodInfo.Name);
                    
                    var permission = new SolhigsonPermission();
                    permission = permissionAttribute.Adapt(permission);
                    if (permission.IsMenuRoot)
                    {
                        permission.IsMenu = true;
                    }

                    if (permission.IsMenu && !(actionInfo as ControllerActionDescriptor).IsApiController())
                    {
                        permission.Url = actionInfo?.AttributeRouteInfo?.Template;
                        if (!string.IsNullOrWhiteSpace(permission.Url))
                        {
                            permission.Url = $"~/{permission.Url}";
                        }
                    }

                    permissionList.Add(permission.Name, permission);
                }
            }

            if (customPermissions != null && customPermissions.Any())
            {
                foreach (var key in customPermissions.Keys.Where(key => !permissionList.ContainsKey(key)
                                                                        && !_dbContext.Permissions.Any(t =>
                                                                            t.Name == key)))
                {
                    permissionList.Add(key, new SolhigsonPermission
                    {
                        Name = key,
                        Description = customPermissions[key],
                    });
                }
            }

            foreach (var permission in from key in permissionList.Keys select permissionList[key])
            {
                permission.Enabled = true;
                _dbContext.Permissions.Add(permission);
                try
                {
                    await _dbContext.SaveChangesAsync();
                    count++;
                    this.ELogInfo(
                        $"Discovered permission protected endpoint: [{permission.Name}] - [{permission.Url}]");
                }
                catch (Exception e)
                {
                    this.ELogError(e, "Saving permission", permission);
                }
            }


            return response.Success(count);
        }
        catch (Exception e)
        {
            this.ELogError(e);
        }

        return response.Fail();
    }


}