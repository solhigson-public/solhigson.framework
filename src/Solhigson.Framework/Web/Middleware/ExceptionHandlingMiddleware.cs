using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Web.Middleware
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private static LogWrapper _logger = LogManager.GetLogger(nameof(ExceptionHandlingMiddleware));
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                if (e is SessionExpiredException)
                {
                    await HandleExceptionAsync(context, StatusCodes.Status401Unauthorized);
                }
                _logger.Error(e);
                await HandleExceptionAsync(context);
            }
        }
        
        private static async Task HandleExceptionAsync(HttpContext httpContext, int statusCode = StatusCodes.Status500InternalServerError)
        {
            try
            {
                httpContext.Response.StatusCode = statusCode;
                if (!httpContext.Response.Body.CanWrite)
                {
                    return;
                }
                if (httpContext.IsApiController())
                {
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(ResponseInfo.FailedResult("Internal Server Error")
                        .SerializeToJson());
                }
                else
                {
                    httpContext.Response.Redirect($"~/_{statusCode}");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Exception thrown in {nameof(ExceptionHandlingMiddleware)}");
            }
        }
    }
}