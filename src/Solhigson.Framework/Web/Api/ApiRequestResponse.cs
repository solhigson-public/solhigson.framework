using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Solhigson.Framework.Web.Api;
public enum RequestOutcome
{
    Success,
    HttpError,
    VendorNetworkLikeHttpError,   // e.g., 522, 599
    TransportNetworkError,        // DNS/TCP/TLS/timeout etc. (no HTTP response)
}

public struct HttpCallResult
{
    public HttpCallResult()
    {
    }

    public RequestOutcome Outcome { get; init; } = RequestOutcome.HttpError;
    public HttpStatusCode StatusCode { get; private init; } = HttpStatusCode.InternalServerError;
    public string? Reason { get; init; }
    public bool IsRetryable { get; init; }
    public string? ErrorType { get; init; }
    public static HttpCallResult New(RequestOutcome outcome, HttpStatusCode statusCode, string? reason = null, bool isRetryable = false, string? errorType = null)
    {
        var result = new HttpCallResult
        {
            Outcome = outcome,
            StatusCode = statusCode,
            Reason = reason,
            IsRetryable = isRetryable,
            ErrorType = errorType
        };
        return result;
    }
}

public class ApiRequestResponse
{
    public HttpStatusCode HttpStatusCode => HttpCallResult.StatusCode;

    public string? HttpStatusDescription { get; set; }

    public string? Response { get; set; }

    public string? Request { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public bool IsSuccessful => IsSuccessfulStatusCode((int) HttpStatusCode);

    public HttpResponseMessage? HttpResponseMessage { get; set; }

    public Dictionary<string, string>? RequestHeaders { get; set; }
    public Dictionary<string, string>? ResponseHeaders { get; set; }

    public bool IsTimeout => this.HttpStatusCode is HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout;

    private static bool IsSuccessfulStatusCode(int statusCode)
    {
        return statusCode is >= 200 and < 300;
    }
        
    public TimeSpan TimeTaken { get; set; }

    public HttpCallResult HttpCallResult { get; set; }
}

public class ApiRequestResponse<T> : ApiRequestResponse
{
    public T? Result { get; set; }
}