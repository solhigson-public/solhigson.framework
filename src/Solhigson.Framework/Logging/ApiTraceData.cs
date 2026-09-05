using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Solhigson.Framework.Logging;

/// <summary>
/// Which side of the wire produced a trace: an outbound call this service made, or an inbound
/// request this service served.
/// </summary>
public enum ApiTraceDirection
{
    Outbound,
    Inbound
}

public class ApiTraceData
{
    public const string UserHttpHeaderIdentifier = "cm-user-email";
    public string Caller { get; set; }
    public string Method { get; set; }

    [JsonIgnore] public string Url { get; set; }

    public string StatusCode { get; set; }
    public string StatusCodeDescription { get; set; }
    public string? RequestMessage { get; set; }
    public Dictionary<string, string> RequestHeaders { get; set; }
    public string? ResponseMessage { get; set; }
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ResponseTime { get; set; }
    public string TimeTaken { get; set; }
    public double TimeSeconds { get; set; }

    /// <summary>Outbound: the remote service (or its host). Inbound: always <c>"Self"</c>.</summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Outbound: <c>Internal</c> / <c>External</c>. Inbound: <c>ServiceStatus</c> — the literals the
    /// two call sites logged before the sink seam; consumers filter on them.
    /// </summary>
    public string? ServiceType { get; set; }

    /// <summary>The human description of the call, logged as <c>{description}</c>.</summary>
    public string? ServiceDescription { get; set; }

    /// <summary><c>Up</c> or <c>Down</c> (<c>Constants.ServiceStatus</c>).</summary>
    public string? Status { get; set; }

    public ApiTraceDirection Direction { get; set; }

    /// <summary>Correlation id from the current log scope; null outside a request scope.</summary>
    public string? ChainId { get; set; }

    /// <summary>
    /// Outbound: the current log scope's user email. Inbound: the <c>cm-user-email</c> request header.
    /// Null when neither is present.
    /// </summary>
    public string? UserIdentity { get; set; }

    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }

    internal string GetUserIdentity()
    {
        string userIdentity = null;

        if (RequestHeaders != null &&
            RequestHeaders.TryGetValue(UserHttpHeaderIdentifier, out var value))
        {
            userIdentity = value;
        }

        return userIdentity;
    }
}
