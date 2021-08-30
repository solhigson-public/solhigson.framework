﻿using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Web.Middleware
{
    public class PermissionsMiddleware : IMiddleware
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        public PermissionsMiddleware(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var endPoint = context.GetEndpoint();
            if (endPoint == null)
            {
                await next(context);
                return;
            }

            if (endPoint.Metadata.GetMetadata<AuthorizeAttribute>() == null && endPoint.Metadata
                .GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo
                .GetCustomAttribute<AuthorizeAttribute>() == null)
            {
                await next(context);
                return;
            }
            
            var permissionName = endPoint.Metadata
                .GetMetadata<PermissionAttribute>()?.Name;
            
            if (string.IsNullOrWhiteSpace(permissionName))
            {
                await next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == false)
            {
                await HandleForbidden(context, "User not authenticated.");
                return;
            }

            var permission = _repositoryWrapper.PermissionRepository.GetByNameCached(permissionName);
            if (permission is null)
            {
                await HandleForbidden(context, "Resource not configured.");
                return;
            }

            var roleIds = context.User.FindAll(Constants.ClaimType.RoleId)
                .Where(t => !string.IsNullOrWhiteSpace(t.Value)).Select(t => t.Value).ToList();
            
            if (!roleIds.Any())
            {
                await HandleForbidden(context, "User not assigned any roles.");
                return;
            }

            if (roleIds.Any(roleId => _repositoryWrapper.RolePermissionRepository
                .GetByRoleIdAndPermissionIdCached(roleId, permission.Id) is not null))
            {
                await next(context);
                return;
            }

            await HandleForbidden(context);
        }

        private static async Task HandleForbidden(HttpContext httpContext, string message = null)
        {
            var msg = "Access to resource is denied";
            msg = !string.IsNullOrWhiteSpace(message) 
                ? $"{msg} - {message}" 
                : $"{msg}.";
            
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            
            if (httpContext.IsApiController())
            {
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(ResponseInfo.FailedResult(msg, StatusCode.UnAuthorised)
                    .SerializeToJson());
            }
        }
    }
}