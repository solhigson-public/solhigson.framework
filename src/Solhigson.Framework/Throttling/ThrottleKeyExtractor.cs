using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Solhigson.Framework.Throttling;

public static class ThrottleKeyExtractor
{
    public static string GetClientKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        return $"ip:{GetClientIp(context)}";
    }

    /// <summary>
    /// The caller address a throttle bucket keys on: the address the CONNECTION resolved to,
    /// <c>HttpContext.Connection.RemoteIpAddress</c>. NO REQUEST HEADER IS READ HERE, and none
    /// may be added.
    /// </summary>
    /// <remarks>
    /// <para>
    /// THE HOST CONFIGURES TRUST, NOT THIS METHOD. A deployment behind a proxy or CDN expresses
    /// which hops it trusts through ASP.NET Core's forwarded-headers middleware
    /// (<c>Configure&lt;ForwardedHeadersOptions&gt;</c> with <c>KnownProxies</c>/<c>KnownNetworks</c>
    /// plus <c>app.UseForwardedHeaders()</c>, registered ahead of the throttle middleware). That
    /// middleware rewrites <c>Connection.RemoteIpAddress</c> before the throttle runs, so this
    /// method already sees the resolved caller and never re-parses the header itself.
    /// </para>
    /// <para>
    /// WHY A HEADER READ CANNOT BE MADE SAFE HERE — this is the second place someone will try to
    /// "improve" it back, so the reason is written down. The forwarded-headers middleware consumes
    /// entries from the RIGHT of <c>X-Forwarded-For</c>, the end each trusted hop appends to, and
    /// under any sane configuration it LEAVES caller-supplied leftmost entries in place. Precisely:
    /// it consumes at most <c>ForwardLimit</c> entries (default 1) and stops at the first hop whose
    /// address is not a <c>KnownProxy</c>/<c>KnownNetwork</c>; everything to the left of where it
    /// stopped survives untouched. Whatever remains on the left is therefore untrusted text the
    /// original caller wrote. (The one configuration that consumes the whole list — both known
    /// lists cleared AND <c>ForwardLimit = null</c> — makes the middleware itself write the
    /// caller's forged leftmost value into <c>RemoteIpAddress</c>. That does not weaken the rule
    /// below; it is the host having discarded its own trust boundary, and this method is then
    /// fooled only because the host was, which is still exactly one place deciding trust.) Any key
    /// derived from the header here is thus choosable by the caller: they can pick their own
    /// bucket, pick a fresh one on every request and escape the limit entirely, or name another
    /// client's address and impose the limit on them. Picking a different entry (rightmost, or
    /// n-from-the-right) does not fix it either — that re-implements, in a second place, the trust
    /// decision the host already expressed in <c>ForwardedHeadersOptions</c>, and two components
    /// deciding trust is exactly how the original bypass happened.
    /// </para>
    /// <para>
    /// NO OPTION IS ADDED TO <see cref="ThrottleOptions"/> FOR THIS. A trusted-proxy list belongs
    /// to the host's forwarded-headers configuration, which every ASP.NET Core deployment already
    /// has, not to the throttle. A second list would drift from the first and reintroduce the same
    /// split-trust defect under a new name.
    /// </para>
    /// <para>
    /// THE ACCEPTED REGRESSION, STATED PLAINLY: a consumer running behind a proxy WITHOUT
    /// forwarded-headers middleware now throttles every client as the proxy's address — one shared
    /// bucket for the whole site. That is over-throttling: it is visible, it fails closed, and it
    /// is fixed by configuring <c>UseForwardedHeaders</c>, which such a deployment needs for
    /// logging and geo-resolution regardless. It replaces a silent bypass in which any caller
    /// sending a header escaped the limit, and a visible over-throttle is the correct failure of
    /// the two.
    /// </para>
    /// <para>
    /// A NULL REMOTE ADDRESS COLLAPSES TO ONE SHARED <c>"unknown"</c> BUCKET, DELIBERATELY.
    /// <c>RemoteIpAddress</c> is null on non-TCP transports and in unit tests; a single bucket can
    /// only over-throttle, never let a caller escape.
    /// </para>
    /// </remarks>
    public static string GetClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
