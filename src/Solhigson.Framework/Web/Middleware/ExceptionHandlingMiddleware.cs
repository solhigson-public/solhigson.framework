﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Utilities;
using Solhigson.Utilities.Dto;

namespace Solhigson.Framework.Web.Middleware;

public class ExceptionHandlingMiddleware : IMiddleware
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(ExceptionHandlingMiddleware).FullName);
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
            Logger.LogError(e);
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
                    .SerializeToJson()!);
            }
            else
            {
                httpContext.Response.Redirect($"{HttpUtils.UrlRoot(httpContext)}/{statusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"Exception thrown in {nameof(ExceptionHandlingMiddleware)}");
        }
    }
}