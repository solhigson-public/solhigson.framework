using System;
using System.Collections.Generic;
using System.Net.Http;
using Solhigson.Utilities;

namespace Solhigson.Framework.Web.Api;

public class ApiRequest
{
    private Dictionary<string, string>? _headers;
    private object? _body;

    private ApiRequest(string uri, HttpMethod method, object? body = null)
    {
        Uri = new Uri(uri);
        HttpMethod = method;
        _body = body;
        Payload = ResolvePayload();
    }

    public static ApiRequest Get(string uri)
        => new(uri, HttpMethod.Get);

    public static ApiRequest Post(string uri, object? body = null)
        => new(uri, HttpMethod.Post, body);

    public static ApiRequest Put(string uri, object? body = null)
        => new(uri, HttpMethod.Put, body);

    public static ApiRequest Delete(string uri, object? body = null)
        => new(uri, HttpMethod.Delete, body);

    public static ApiRequest Patch(string uri, object? body = null)
        => new(uri, HttpMethod.Patch, body);

    #region Fluent Configuration

    public ApiRequest WithHeader(string key, string value)
    {
        _headers ??= new Dictionary<string, string>();
        _headers.TryAdd(key, value);
        return this;
    }

    public ApiRequest WithHeaders(IDictionary<string, string> headers)
    {
        _headers ??= new Dictionary<string, string>();
        foreach (var (key, value) in headers)
        {
            _headers.TryAdd(key, value);
        }
        return this;
    }

    public ApiRequest WithBearerToken(string token)
        => WithHeader("Authorization", $"Bearer {token}");

    public ApiRequest WithNamedClient(string clientName)
    {
        NamedHttpClient = clientName;
        return this;
    }

    public ApiRequest WithTimeout(int seconds)
    {
        TimeOut = seconds;
        return this;
    }

    public ApiRequest WithContentType(string contentType)
    {
        Format = contentType;
        return this;
    }

    public ApiRequest WithExpectContinue(bool value = true)
    {
        ExpectContinue = value;
        return this;
    }

    public ApiRequest WithLogTrace(bool value = true)
    {
        LogTrace = value;
        return this;
    }

    public ApiRequest WithServiceName(string name)
    {
        ServiceName = name;
        return this;
    }

    public ApiRequest WithServiceDescription(string description)
    {
        ServiceDescription = description;
        return this;
    }

    public ApiRequest WithServiceType(string type)
    {
        ServiceType = type;
        return this;
    }

    public ApiRequest WithHttpContent(HttpContent content)
    {
        HttpContent = content;
        return this;
    }

    public ApiRequest AsFormUrlEncoded()
        => WithContentType(ContentTypes.FormUrlEncoded);

    public ApiRequest AsXml()
        => WithContentType(ContentTypes.Xml);

    public ApiRequest WithoutResponseContent()
    {
        ReadResponseContent = false;
        return this;
    }

    #endregion

    #region Properties

    public bool ExpectContinue { get; private set; }
    public Uri Uri { get; }
    public HttpMethod HttpMethod { get; }
    public IReadOnlyDictionary<string, string>? Headers => _headers;
    public string Format { get; private set; } = ContentTypes.Json;
    public int TimeOut { get; private set; }
    public string? Payload { get; }
    public string? ServiceName { get; private set; }
    public string? ServiceType { get; private set; }
    public string? ServiceDescription { get; private set; }
    public string? NamedHttpClient { get; private set; }
    public HttpContent? HttpContent { get; private set; }
    public bool ReadResponseContent { get; private set; } = true;
    public bool? LogTrace { get; private set; }

    #endregion

    private string? ResolvePayload()
    {
        if (_body is null)
        {
            return null;
        }

        if (_body is string s)
        {
            return s;
        }

        if (_body is IDictionary<string, string> formData)
        {
            Format = ContentTypes.FormUrlEncoded;
            using var content = new FormUrlEncodedContent(formData);
            return content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        return _body.SerializeToJson();
    }
}
