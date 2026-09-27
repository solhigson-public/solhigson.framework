using System;
using System.Collections.Generic;

namespace Solhigson.Framework.Web.Api;

public record ApiConfiguration
{
    public bool LogOutBoundApiRequests { get; set; }

    [Obsolete("unread; use InboundTraceEnabled")]
    public bool LogInBoundApiRequests { get; set; }

    /// <summary>
    /// Master switch for inbound tracing, called on every request so a runtime change takes effect
    /// without a restart. Null means off.
    /// </summary>
    public Func<bool>? InboundTraceEnabled { get; set; }

    /// <summary>
    /// Path patterns an inbound request must match at least one of to be traced, for example
    /// <c>/api</c> or <c>/api/contests/{id}/live-results</c>. Matched against the request path only,
    /// by <c>/</c> segment, ignoring case; a <c>{name}</c> segment matches exactly one segment; a
    /// pattern covers its path and everything beneath it. Called per request while the switch is on;
    /// return the same instance until the list changes so the compiled patterns are reused. Never
    /// mutate a returned collection; return a new instance when the list changes; in-place changes are
    /// never observed. Null or empty means nothing is traced inbound: there is no fallback.
    /// </summary>
    public Func<IReadOnlyCollection<string>>? InboundIncludePaths { get; set; }

    /// <summary>
    /// Path patterns, in the same form as <see cref="InboundIncludePaths"/>, that stop an included
    /// request from being traced. Exclude wins over include. Called per request only for a request the
    /// include list matched. Never mutate a returned collection; return a new instance when the list
    /// changes; in-place changes are never observed. Null means empty.
    /// </summary>
    public Func<IReadOnlyCollection<string>>? InboundExcludePaths { get; set; }
}
