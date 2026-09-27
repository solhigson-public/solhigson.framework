using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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
    private readonly LogWrapper _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly IApiTraceSink _sink;
    private readonly ApiConfiguration? _apiConfiguration;
    private readonly InboundTracePathPatterns _includePatterns;
    private readonly InboundTracePathPatterns _excludePatterns;

    // The configuration comes from the container (SolhigsonAutofacModule registers a default instance,
    // a consumer's own registration wins); both are singletons, which is why the switch and the lists
    // are functions called per request rather than values read here.
    // Both parameters are optional so that no construction path can break a request: a consumer that
    // registers the type itself (plain MS-DI, no ApiConfiguration registered) gets a null
    // configuration, which means inbound tracing is OFF, and a consumer with no IApiTraceSink gets the
    // log-emitting default.
    public ApiTraceMiddleware(ApiConfiguration? apiConfiguration = null, IApiTraceSink? sink = null)
        : this(apiConfiguration, sink, null)
    {
    }

    /// <summary>
    /// Test seam: <paramref name="loggerFactory"/> builds an unshared logger under the same name, since
    /// <see cref="Logging.LogManager"/> caches one wrapper per name for the life of the process.
    /// </summary>
    internal ApiTraceMiddleware(ApiConfiguration? apiConfiguration, IApiTraceSink? sink,
        ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory is null
            ? Logging.LogManager.GetLogger(nameof(ApiTraceMiddleware))
            : new LogWrapper(nameof(ApiTraceMiddleware), loggerFactory);
        _apiConfiguration = apiConfiguration;
        _sink = sink ?? new LoggingApiTraceSink();
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _includePatterns = new InboundTracePathPatterns("include", _logger);
        _excludePatterns = new InboundTracePathPatterns("exclude", _logger);
    }

    internal InboundTracePathPatterns IncludePatterns => _includePatterns;
    internal InboundTracePathPatterns ExcludePatterns => _excludePatterns;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!ShouldTraceInbound(context.Request))
        {
            await next(context);
            return;
        }

        var url = context.Request.GetDisplayUrl();
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
                _logger.LogError(e, "While saving api trace data for url: {url}", traceData.Url);
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

    /// <summary>
    /// Traces only when the switch returns true, the request path matches at least one include pattern
    /// and no exclude pattern. Reads <see cref="HttpRequest.Path"/> only, never host or query string.
    /// The switch is read first and the list functions are not called while it is off. There is no
    /// fallback: a null or empty include list traces nothing, and so does a null configuration. A
    /// throwing consumer function degrades to no trace, since tracing must never break the request it
    /// would trace.
    /// </summary>
    private bool ShouldTraceInbound(HttpRequest request)
    {
        if (_apiConfiguration is null)
        {
            return false;
        }

        try
        {
            var enabled = _apiConfiguration.InboundTraceEnabled;
            if (enabled is null || !enabled())
            {
                return false;
            }

            var include = _apiConfiguration.InboundIncludePaths;
            if (include is null)
            {
                return false;
            }

            var path = request.Path.Value.AsSpan();
            if (!_includePatterns.AnyMatch(include(), path))
            {
                return false;
            }

            var exclude = _apiConfiguration.InboundExcludePaths;
            return exclude is null || !_excludePatterns.AnyMatch(exclude(), path);
        }
        catch (OperationCanceledException e)
        {
            // A consumer function observing an aborted request's cancellation is not a fault.
            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, "Cancelled while deciding whether to trace inbound path: {path}", e,
                request.Path.Value);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "While deciding whether to trace inbound path: {path}", request.Path.Value);
            return false;
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