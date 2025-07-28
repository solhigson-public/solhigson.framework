using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Solhigson.Framework.Web.Api;

public class ApiRequestDetails(Uri uri, HttpMethod httpMethod, string? payload = null, Dictionary<string, string>? headers = null)
{
    private Dictionary<string, string>? _headers = headers;

    public void AddHeader(string key, string value)
    {
        _headers ??= new Dictionary<string, string>();
        _headers.TryAdd(key, value);
    }
    public bool ExpectContinue { get; set; } = true;
    public Uri Uri { get; } = uri;
    public HttpMethod HttpMethod { get; set; } = httpMethod;
    public IReadOnlyDictionary<string, string>? Headers => _headers;
    public string Format { get; set; } = ApiRequestService.ContentTypeJson;
    public int TimeOut { get; set; } = 0;
    public string? Payload { get; } = payload;
    public string? ServiceName { get; set; }
    public string? ServiceType { get; set; }
    public string? ServiceDescription { get; set; }
        
    public string? NamedHttpClient { get; set; }
    
    public bool? LogTrace { get; set; }
}