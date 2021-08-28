using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Persistence.Repositories;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Web.Attributes;
using Xunit;

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

            var roleId = context.User.FindFirstValue(Constants.ClaimType.RoleId);
            if (string.IsNullOrWhiteSpace(roleId))
            {
                await HandleForbidden(context, "User not assigned a role.");
                return;
            }
            
            var isAllowed = _repositoryWrapper.RolePermissionRepository
                .GetByRoleIdAndPermissionIdCached(roleId, permission.Id);
            if (isAllowed is null)
            {
                await HandleForbidden(context);
                return;
            }
            await next(context);
        }

        private static async Task HandleForbidden(HttpContext httpContext, string message = null)
        {
            var msg = "Access to resource is denied";
            if (!string.IsNullOrWhiteSpace(message))
            {
                msg = $"{msg} - {message}";
            }
            else
            {
                msg = $"{msg}.";
            }
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