using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Web.Middleware
{
    public class SolhigsonExceptionHandlingMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                this.ELogError(e);
                await HandleExceptionAsync(context);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext httpContext)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync(ResponseInfo.FailedResult("Internal Server Error").SerializeToJson());
        }
    }
}