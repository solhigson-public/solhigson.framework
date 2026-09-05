using Microsoft.Extensions.Logging;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Web.Middleware;

namespace Solhigson.Framework.Web.Api;

/// <summary>
/// Default <see cref="IApiTraceSink"/>: emits the trace through the same logger, at the same level,
/// with the same message template and the same six arguments that <see cref="ApiRequestService"/> and
/// <see cref="ApiTraceMiddleware"/> emitted inline before the sink seam existed.
/// </summary>
/// <remarks>
/// The two logger names are load-bearing: they are persisted downstream as <c>AppLog.Logger</c> and
/// consumers query and route (NLog rules) on them, so they must not drift from
/// <c>nameof(ApiRequestService)</c> (outbound) and <c>nameof(ApiTraceMiddleware)</c> (inbound).
/// </remarks>
public sealed class LoggingApiTraceSink : IApiTraceSink
{
    /// <summary>The template both call sites used verbatim before the seam.</summary>
    internal const string MessageTemplate =
        "{description}, {url}, {serviceName}, {serviceType}, {status}, {traceData}";

    internal const string OutboundLoggerName = nameof(ApiRequestService);
    internal const string InboundLoggerName = nameof(ApiTraceMiddleware);

    private readonly LogWrapper _outboundLogger;
    private readonly LogWrapper _inboundLogger;

    public LoggingApiTraceSink() : this(null)
    {
    }

    /// <summary>
    /// Test seam. <see cref="LogManager.GetLogger(string, ILoggerFactory)"/> caches one
    /// <see cref="LogWrapper"/> per name for the life of the process and binds it to whatever factory
    /// was current at first use, so a test cannot capture emissions through it deterministically;
    /// passing a factory here builds unshared wrappers under the SAME names instead.
    /// </summary>
    internal LoggingApiTraceSink(ILoggerFactory? loggerFactory)
    {
        _outboundLogger = loggerFactory is null
            ? LogManager.GetLogger(OutboundLoggerName)
            : new LogWrapper(OutboundLoggerName, loggerFactory);

        _inboundLogger = loggerFactory is null
            ? LogManager.GetLogger(InboundLoggerName)
            : new LogWrapper(InboundLoggerName, loggerFactory);
    }

    public void Save(ApiTraceData trace)
    {
        var logger = trace.Direction == ApiTraceDirection.Inbound
            ? _inboundLogger
            : _outboundLogger;

        logger.LogInformation(MessageTemplate, trace.ServiceDescription, trace.Url, trace.ServiceName,
            trace.ServiceType, trace.Status, trace);
    }
}
