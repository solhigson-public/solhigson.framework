using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Solhigson.Framework.Logging;

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
