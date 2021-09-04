﻿using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Services.Abstractions;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Web.Middleware
{
    public class PermissionsMiddleware : IMiddleware
    {
        private readonly IPermissionService _permissionService;
        public PermissionsMiddleware(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var endPoint = context.GetEndpoint();
            if (endPoint == null)
            {
                await next(context);
                return;
            }

            if ((endPoint.Metadata.GetMetadata<AuthorizeAttribute>() == null 
                && endPoint.Metadata.GetMetadata<ControllerActionDescriptor>()?
                    .ControllerTypeInfo?.GetCustomAttribute<AuthorizeAttribute>() == null)
                || endPoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await next(context);
                return;
            }
            
            var permissionName = endPoint.Metadata
                .GetMetadata<PermissionAttribute>()?.Name;
            
            if (string.IsNullOrWhiteSpace(permissionName))
            {
                if (string.IsNullOrWhiteSpace(permissionName))
                {
                    await next(context);
                    return;
                }
            }

            var verifyResult = _permissionService.VerifyPermission(permissionName, context.User);
            
            if (!verifyResult.IsSuccessful)
            {
                await HandleForbidden(context, verifyResult.Message);
                return;
            }
            await next(context);
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