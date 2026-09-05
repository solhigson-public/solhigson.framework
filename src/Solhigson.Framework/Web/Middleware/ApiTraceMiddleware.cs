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
using Solhigson.Framework.Web.Api;
using Solhigson.Utilities;

namespace Solhigson.Framework.Web.Middleware;

public sealed class ApiTraceMiddleware : IMiddleware
{
    private static readonly LogWrapper Logger = Logging.LogManager.GetLogger(nameof(ApiTraceMiddleware));
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly IApiTraceSink _sink;

    // Optional so that no existing construction path can break: the middleware is only ever resolved
    // from the container (Autofac injects the registered sink), but a consumer that registers the type
    // itself without an IApiTraceSink still gets the log-emitting default, i.e. today's behaviour.
    public ApiTraceMiddleware(IApiTraceSink? sink = null)
    {
        _sink = sink ?? new LoggingApiTraceSink();
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

        try
        {
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

            // The literals below are exactly what this middleware logged as {serviceName} / {serviceType}
            // before the sink seam existed; consumers filter on them, so they are carried on the payload
            // rather than re-derived.
            traceData.Direction = ApiTraceDirection.Inbound;
            traceData.ServiceName = Constants.ServiceType.Self;
            traceData.ServiceType = Constants.Group.ServiceStatus;
            traceData.ServiceDescription = desc;
            traceData.Status = status;
            traceData.ChainId = this.GetCurrentLogChainId();
            traceData.UserIdentity = traceData.GetUserIdentity();

            // A consumer-replaceable sink must never break the request it is tracing, and must never
            // skip the response copy below: a throwing sink degrades to no trace.
            try
            {
                _sink.Save(traceData);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "While saving api trace data for url: {url}", traceData.Url);
            }

            //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Restore the real response stream however next() exited. If it THREW, the buffer above is
            // disposed on the way out of this method while context.Response.Body still pointed at it,
            // and the outer exception handler (which sits outside this middleware) would write its
            // error payload into a disposed stream: a client-visible empty 500. The buffered bytes are
            // deliberately NOT copied out on that path — the response is half-written and unstarted, so
            // the handler gets a clean stream to write its own body into. The exception itself is
            // untouched and propagates unchanged.
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<ApiTraceData> GetRequestData(HttpRequest request, string url)
    {
        string requestContent;
        await using (var bodyStream = _recyclableMemoryStreamManager.GetStream())
        {
            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableBuffering();

            //Copy the whole body out, then read the copy back in full, taking its length from the copy
            //itself. Content-Length is ABSENT on a chunked or streamed request, so the buffer this used
            //to size from it came out empty and the body was traced as "" with no error at all.
            //Same shape as GetResponseData below.
            await request.Body.CopyToAsync(bodyStream);
            bodyStream.Position = 0;

            using (var reader = new StreamReader(bodyStream, Encoding.UTF8, leaveOpen: true))
            {
                requestContent = await reader.ReadToEndAsync();
            }

            //Rewind the request body so the rest of the pipeline still reads it.
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