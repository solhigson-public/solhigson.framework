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
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Persistence.EntityModels;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Services.Abstractions;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Services
{
    public class PermissionService : ServiceBase, IPermissionService
    {
        public IActionDescriptorCollectionProvider ActionDescriptorCollectionProvider { get; set; }
        public PermissionService(IRepositoryWrapper repositoryWrapper) : base(repositoryWrapper)
        {
        }

        public ResponseInfo VerifyPermission(string permissionName, ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal?.Identity?.IsAuthenticated == false)
            {
                return ResponseInfo.FailedResult("User not authenticated.");
            }

            var permission = RepositoryWrapper.PermissionRepository.GetByNameCached(permissionName);
            if (permission is null)
            {
                return ResponseInfo.FailedResult("Resource not configured.");
            }

            var roleIds = claimsPrincipal?.FindAll(Constants.ClaimType.RoleId)
                .Where(t => !string.IsNullOrWhiteSpace(t.Value)).Select(t => t.Value).ToList();
            
            if (roleIds == null || !roleIds.Any())
            {
                return ResponseInfo.FailedResult("User not assigned any roles.");
            }

            return roleIds.Any(roleId => RepositoryWrapper.RolePermissionRepository
                .GetByRoleIdAndPermissionIdCached(roleId, permission.Id) is not null) 
                ? ResponseInfo.SuccessResult() 
                : ResponseInfo.FailedResult();
        }

        public async Task AddPermission(PermissionDto permissionDto)
        {
            RepositoryWrapper.PermissionRepository.Add(permissionDto.Adapt<Permission>());
            await RepositoryWrapper.SaveChangesAsync();
        }

        public async Task<IList<PermissionDto>> GetAllPermissionsAsync()
        {
            return await RepositoryWrapper.PermissionRepository.GetAll().ProjectToType<PermissionDto>().ToListAsync();
        }

        public async Task<IList<PermissionDto>> GetAllPermissionsForRoleAsync(string roleId)
        {
            return await (from p in RepositoryWrapper.DbContext.Permissions
                join rp in RepositoryWrapper.DbContext.RolePermissions
                    on p.Id equals rp.PermissionId
                where rp.RoleId == roleId
                select p).ProjectToType<PermissionDto>().ToListAsync();
        }
        
        public IList<PermissionDto> GetAllPermissionsForRoleCached(string roleId)
        {
            return RepositoryWrapper.DbContext.RolePermissions.Include(t => t.Permission)
                .Where(t => t.RoleId == roleId)
                .Select(t => t.Permission).ProjectToType<PermissionDto>().FromCacheList();
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

                    if (RepositoryWrapper.PermissionRepository.Exists(t =>
                        t.Name == permissionAttribute.Name))
                    {
                        continue;
                    }
                    
                    var actionInfo = ActionDescriptorCollectionProvider.ActionDescriptors.Items.FirstOrDefault(x => (x is ControllerActionDescriptor controllerActionDescriptor)
                        && controllerActionDescriptor.ControllerTypeInfo.AsType() == controllerType
                        && controllerActionDescriptor.ActionName == methodInfo.Name);

                    var permission = permissionAttribute.Adapt<Permission>();
                    permission.Id = Guid.NewGuid().ToString();
                    RepositoryWrapper.PermissionRepository.Add(permission);
                    permission.Url = actionInfo?.AttributeRouteInfo?.Template;
                    count++;
                }
            }
            await RepositoryWrapper.SaveChangesAsync();
            return response.Success(count);
        }


    }
}