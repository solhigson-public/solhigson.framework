using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.IO;
using NLog;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Web.Middleware;

public sealed class ApiTraceMiddleware : IMiddleware
{
    private static readonly LogWrapper Logger = new (nameof(ApiTraceMiddleware));
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public ApiTraceMiddleware()
    {
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var url = context.Request.GetDisplayUrl();
        if (!url.ToLower().Contains("api/")) //only log api calls [hack, should fix this later :)]
        {
            await next(context);
            return;
        }

        var traceData = await GetRequestData(context.Request, url);

        //Copy a pointer to the original response body stream
        var originalBodyStream = context.Response.Body;

        //Create a new memory stream...
        await using var responseBody = _recyclableMemoryStreamManager.GetStream();
        //...and use that for the temporary response body
        context.Response.Body = responseBody;

        //Continue down the Middleware pipeline, eventually returning to this class
        await next(context);

        //Format the response from the server
        await GetResponseData(context.Response, traceData);

        var status = HelperFunctions.IsServiceUp(context.Response)
            ? Constants.ServiceStatus.Up
            : Constants.ServiceStatus.Down;

        var action = context.GetRouteData().Values["action"]?.ToString();
        var desc = string.IsNullOrWhiteSpace(action)
            ? "Inbound"
            : HelperFunctions.SeparatePascalCaseWords(action);

        this.SetCurrentLogUserEmail(traceData.GetUserIdentity());
        Logger.Log(desc, LogLevel.Info, traceData, null,
            null, Constants.ServiceType.Self,
            Constants.Group.ServiceStatus, status, traceData.Url);

        //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private async Task<ApiTraceData> GetRequestData(HttpRequest request, string url)
    {
        string requestContent;
        await using (var bodyStream = _recyclableMemoryStreamManager.GetStream())
        {
            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableBuffering();

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            await request.Body.CopyToAsync(bodyStream);
            bodyStream.Position = 0;

            await bodyStream.ReadAsync(buffer, 0, buffer.Length);

            requestContent = Encoding.UTF8.GetString(buffer);

            request.Body.Position = 0;
        }

        var method = request.Method.ToUpper();

        return new ApiTraceData
        {
            RequestTime = DateTime.UtcNow,
            Url = url,
            Method = method,
            /*
            RequestMessage = HelperFunctions.CheckForProtectedFields(requestContent, _servicesWrapper),
            */
            RequestMessage = requestContent,
            Caller = HelperFunctions.GetCallerIp(request.HttpContext),
            RequestHeaders = HelperFunctions.ToJsonObject(request.Headers)
        };
    }

    private static async Task GetResponseData(HttpResponse response, ApiTraceData traceData)
    {
        //We need to read the response stream from the beginning...
        response.Body.Seek(0, SeekOrigin.Begin);

        //...and copy it into a string
        var responseContent = await new StreamReader(response.Body).ReadToEndAsync();

        //We need to reset the reader for the response so that the client can read it.
        response.Body.Seek(0, SeekOrigin.Begin);

        var statusCode = (HttpStatusCode) response.StatusCode;

        //traceData.ResponseMessage = HelperFunctions.CheckForProtectedFields(responseContent, _servicesWrapper);
        traceData.ResponseMessage = responseContent;
        traceData.ResponseTime = DateTime.UtcNow;
        var timeTaken = traceData.ResponseTime - traceData.RequestTime;
        traceData.TimeSeconds = timeTaken.TotalSeconds;
        traceData.TimeTaken = HelperFunctions.TimespanToWords(timeTaken);
        traceData.ResponseHeaders = HelperFunctions.ToJsonObject(response.Headers);

        traceData.StatusCode = response.StatusCode.ToString();
        traceData.StatusCodeDescription = statusCode.ToString();
    }
}