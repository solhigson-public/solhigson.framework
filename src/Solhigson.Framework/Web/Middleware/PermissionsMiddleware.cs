using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Identity;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Web.Middleware
{
    public class PermissionsMiddleware<TUser, TContext> : PermissionsMiddleware<TUser, SolhigsonAspNetRole<string>, string, TContext>
        where TUser : SolhigsonUser
        where TContext : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole<string>, string>
    {
        public PermissionsMiddleware(PermissionManager<TUser, SolhigsonAspNetRole<string>, TContext, string> permissionManager) : base(permissionManager)
        {
        }
    }
    
    public class PermissionsMiddleware<TUser, TKey, TContext> : PermissionsMiddleware<TUser, SolhigsonAspNetRole<TKey>, TKey, TContext>
        where TUser : SolhigsonUser<TKey>
        where TContext : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole<TKey>, TKey>
        where TKey : IEquatable<TKey>
    {
        public PermissionsMiddleware(PermissionManager<TUser, SolhigsonAspNetRole<TKey>, TContext, TKey> permissionManager) : base(permissionManager)
        {
        }
    }


    public class PermissionsMiddleware<TUser, TRole, TKey, TContext> : IMiddleware 
        where TUser : SolhigsonUser<TKey> 
        where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey>
        where TRole : SolhigsonAspNetRole<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly PermissionManager<TUser, TRole, TContext, TKey> _permissionManager;
        public PermissionsMiddleware(PermissionManager<TUser, TRole, TContext, TKey> permissionManager)
        {
            _permissionManager = permissionManager;
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
                await next(context);
                return;
            }

            var verifyResult = _permissionManager.VerifyPermission(permissionName, context.User);
            
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