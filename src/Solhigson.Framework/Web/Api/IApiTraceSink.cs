using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Web.Api;

/// <summary>
/// Receives a completed API trace (outbound call or inbound request) for persistence or emission.
/// </summary>
/// <remarks>
/// <para>Synchronous and called inline, once per trace, from
/// <see cref="ApiRequestService.SaveApiTraceData"/> (outbound) and
/// <see cref="Solhigson.Framework.Web.Middleware.ApiTraceMiddleware"/> (inbound). Both call sites wrap
/// the call in their own catch-all, so an implementation that throws degrades to no trace and never
/// reaches the caller of the HTTP request. An implementation that BLOCKS, however, blocks the request:
/// implementations that do I/O should hand off to their own queue or background job.</para>
/// <para>The default implementation is <see cref="LoggingApiTraceSink"/>, registered with
/// <c>PreserveExistingDefaults()</c> so a consumer registration of <see cref="IApiTraceSink"/> wins.</para>
/// </remarks>
public interface IApiTraceSink
{
    void Save(ApiTraceData trace);
}
