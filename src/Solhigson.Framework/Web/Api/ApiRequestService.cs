using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Web.Api;

public class ApiRequestService : IApiRequestService
{
    public const string ContentTypePlain = "text/plain";
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeXml = "application/xml";
    public const string ContentTypeXWwwFormUrlencoded = "application/x-www-form-urlencoded";
    private readonly LogWrapper _logger = Logging.LogManager.GetLogger("ApiRequestHelper");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiConfiguration _apiConfiguration;
    private Action<ApiConfiguration> _configuration;

    public ApiRequestService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _apiConfiguration = new ApiConfiguration();
    }
    
    public void UseConfiguration(Action<ApiConfiguration> configuration)
    {
        _configuration = configuration;
    }


    #region GET Requests

    public async Task<ApiRequestResponse> GetDataJsonAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Get, null, ContentTypeJson, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse> GetDataXmlAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Get, null, ContentTypeXml, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse> GetDataPlainAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Get, null, ContentTypePlain, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse<T>> GetDataJsonAsync<T>(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) 
    {
        return await SendRequestAsync<T>(uri, HttpMethod.Get, null, ContentTypeJson, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse<T>> GetDataXmlAsync<T>(string uri,
        Dictionary<string, string> headers = null,
        string serviceName = null, string serviceDescription = null, string serviceType = null,  string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
        
    {
        return await SendRequestAsync<T>(uri, HttpMethod.Get, null, ContentTypeXml, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    #endregion

    #region POST Requests

    public async Task<ApiRequestResponse> PostDataJsonAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Post, data, ContentTypeJson, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse> PostDataXmlAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Post, data, ContentTypeXml, headers, serviceName,
            serviceDescription, serviceType, namedHttpClient,
            timeOut, logTrace);
    }

    public async Task<ApiRequestResponse> PostDataAsync(string uri, string data, string contentType,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Post, data, contentType, headers, serviceName,
            serviceDescription, serviceType, namedHttpClient,
            timeOut, logTrace);
    }

    public async Task<ApiRequestResponse<T>> PostDataJsonAsync<T>(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) 
    {
        return await SendRequestAsync<T>(uri, HttpMethod.Post, data, ContentTypeJson, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
        string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) 
    {
        return await SendRequestAsync<T>(uri, HttpMethod.Post, data, ContentTypeXWwwFormUrlencoded, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
        IDictionary<string, string> data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) 
    {
        return await PostDataXWwwFormUrlencodedAsync<T>(uri,
            await new FormUrlEncodedContent(data).ReadAsStringAsync(),
            headers, serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }

    public async Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri,
        IDictionary<string, string> data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await PostDataXWwwFormUrlencodedAsync(uri, await new FormUrlEncodedContent(data).ReadAsStringAsync(),
            headers, serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }


    public async Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync(uri, HttpMethod.Post, data, ContentTypeXWwwFormUrlencoded, headers,
            serviceName, serviceDescription, serviceType, namedHttpClient, timeOut, logTrace);
    }


    public async Task<ApiRequestResponse<T>> PostDataXmlAsync<T>(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) 
    {
        return await SendRequestAsync<T>(uri, HttpMethod.Post, data, ContentTypeXml, headers, serviceName,
            serviceDescription, namedHttpClient,
            serviceType, timeOut, logTrace);
    }

    #endregion

    #region Helpers

    private async Task<ApiRequestResponse> SendRequestAsync(string uri, HttpMethod method,
        string data = "", string format = ContentTypeJson,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null)
    {
        return await SendRequestAsync<object>(uri, method, data, format, headers, serviceName, serviceDescription,
            serviceType, namedHttpClient,
            timeOut, logTrace);
    }

    private async Task<ApiRequestResponse<T>> SendRequestAsync<T>(string uri, HttpMethod method,
        string data = "", string format = ContentTypeJson,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) 
    {
        try
        {
            var apiRequestDetails = new ApiRequestDetails(new Uri(uri), method, data)
            {
                Format = format,
                Headers = headers,
                TimeOut = timeOut,
                ServiceName = serviceName,
                ServiceType = serviceType,
                ServiceDescription = serviceDescription,
                NamedHttpClient = namedHttpClient,
                LogTrace = logTrace
            };
            return await SendRequestAsync<T>(apiRequestDetails);
        }
        catch (Exception e)
        {
            _logger.LogError(e);
        }

        return new ApiRequestResponse<T>();
    }


    public async Task<ApiRequestResponse> SendRequestAsync(ApiRequestDetails apiRequestDetails)
    {
        return await SendRequestAsync<object>(apiRequestDetails);
    }

    public async Task<ApiRequestResponse<T>> SendRequestAsync<T>(ApiRequestDetails apiRequestDetails)
        
    {
        return await SendRequestInternalAsync<T>(apiRequestDetails);
    }

    protected async Task<ApiRequestResponse<T>> SendRequestInternalAsync<T>(
        ApiRequestDetails apiRequestDetails)
    {
        _configuration?.Invoke(_apiConfiguration);
        if (apiRequestDetails.LogTrace is not null)
        {
            _apiConfiguration.LogOutBoundApiRequests = apiRequestDetails.LogTrace.Value;
        }
        var method = apiRequestDetails.HttpMethod;
        var data = apiRequestDetails.Payload;
        var format = apiRequestDetails.Format;
        var timeOut = apiRequestDetails.TimeOut;
        var url = apiRequestDetails.Uri.ToString();

        var serviceName = apiRequestDetails.ServiceName;
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            serviceName = apiRequestDetails.Uri.Host;
        }

        var serviceType = apiRequestDetails.ServiceType;
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            serviceType = Constants.ServiceType.External;
        }

        var apiRequestHelperResponse = new ApiRequestResponse<T>
        {
            StartTime = DateTime.UtcNow
        };
        var client = _httpClientFactory.CreateClient(apiRequestDetails.NamedHttpClient ?? "");
        client.DefaultRequestHeaders.ExpectContinue = apiRequestDetails.ExpectContinue;
        var request = new HttpRequestMessage();

        try
        {
            request.Method = method;
            request.RequestUri = apiRequestDetails.Uri;
            if (!string.IsNullOrWhiteSpace(data))
            {
                request.Content = new StringContent(data, Encoding.UTF8, format);
            }
            else if (method == HttpMethod.Delete || method == HttpMethod.Put)
            {
                request.Content ??= new StringContent("", Encoding.UTF8, format);
            }

            if (timeOut > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeOut);
            }

            if (apiRequestDetails.Headers is { Count: > 0 })
            {
                foreach (var key in apiRequestDetails.Headers.Keys)
                {
                    if (apiRequestDetails.Headers.TryGetValue(key, out var value) &&
                        !string.IsNullOrWhiteSpace(value))
                    {
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }

            apiRequestHelperResponse.Request = request.Content != null
                ? await request.Content.ReadAsStringAsync()
                : data;

            apiRequestHelperResponse.ResponseHeaders = new Dictionary<string, string>();

            apiRequestHelperResponse.StartTime = DateTime.UtcNow;
            apiRequestHelperResponse.HttpResponseMessage = await MakeHttpCall<T>(apiRequestDetails, client, request);// client.SendAsync(request);
            apiRequestHelperResponse.EndTime = DateTime.UtcNow;
            apiRequestHelperResponse.Response =
                await apiRequestHelperResponse.HttpResponseMessage.Content.ReadAsStringAsync();

        }
        catch (WebException we)
        {
            apiRequestHelperResponse.Response = we.Message;
            var hResponse = (HttpWebResponse) we.Response;
            if (hResponse == null)
            {
                _logger.LogError(we, $"While sending request to url: {url}");
                GetStatusCode(we, apiRequestHelperResponse);

                apiRequestHelperResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
                apiRequestHelperResponse.HttpStatusDescription = we.Message;
            }
            else
            {
                await using var stream = hResponse.GetResponseStream();
                if (stream != null)
                {
                    using var sReader = new StreamReader(stream);
                    apiRequestHelperResponse.Response = await sReader.ReadToEndAsync();
                }

                if (hResponse.Headers != null)
                {
                    foreach (var header in hResponse.Headers.AllKeys)
                    {
                        apiRequestHelperResponse.ResponseHeaders.Add(header, hResponse.Headers[header]);
                    }
                }

                apiRequestHelperResponse.HttpStatusCode = hResponse.StatusCode;
                apiRequestHelperResponse.HttpStatusDescription = hResponse.StatusDescription;
            }
        }
        catch (Exception e)
        {
            apiRequestHelperResponse.Response = e.Message;
            _logger.LogError(e, $"While sending request to url: {url}");
            apiRequestHelperResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
            if ((DateTime.UtcNow - apiRequestHelperResponse.StartTime).TotalMilliseconds >= apiRequestDetails.TimeOut)
            {
                apiRequestHelperResponse.HttpStatusCode = HttpStatusCode.RequestTimeout;
            }

            apiRequestHelperResponse.HttpStatusDescription = e.Message;
        }
        finally
        {
            try
            {
                apiRequestHelperResponse.EndTime ??= DateTime.UtcNow;
                apiRequestHelperResponse.TimeTaken = apiRequestHelperResponse.EndTime.Value - apiRequestHelperResponse.StartTime;

                var responseFormat = format;
                JObject responseHeaders = null;
                if (apiRequestHelperResponse.HttpResponseMessage != null)
                {
                    apiRequestHelperResponse.HttpStatusCode =
                        apiRequestHelperResponse.HttpResponseMessage.StatusCode;
                    responseHeaders =
                        HelperFunctions.ToJsonObject(apiRequestHelperResponse.HttpResponseMessage.Headers);
                    if (responseHeaders.TryGetValue("Content-Type", out var header))
                    {
                        responseFormat = header.ToString();
                    }
                    if (apiRequestHelperResponse.HttpResponseMessage.Headers != null)
                    {
                        foreach (var (key, value) in apiRequestHelperResponse.HttpResponseMessage.Headers)
                        {
                            apiRequestHelperResponse.ResponseHeaders.Add(key, string.Join(",", value));
                        }
                    }
                }


                ExtractObject(apiRequestHelperResponse, responseFormat);

                if (_apiConfiguration.LogOutBoundApiRequests)
                {
                    _ = Task.Run(() => SaveApiTraceData(url, method.ToString(), apiRequestDetails.Headers, apiRequestHelperResponse.StartTime, 
                        apiRequestHelperResponse.EndTime ?? DateTime.UtcNow,
                        apiRequestHelperResponse.Request,
                        apiRequestHelperResponse.Response, responseHeaders,
                        apiRequestHelperResponse.HttpStatusCode,
                        apiRequestDetails.ServiceDescription, serviceName, serviceType));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }

        return apiRequestHelperResponse;
    }

    protected virtual void SaveApiTraceData(string url, string method, Dictionary<string, string> requestHeaders, DateTime startTime, DateTime endTime, string requestMessage,
        string responseMessage, JObject? responseHeaders, HttpStatusCode statusCode, string serviceDescription,
        string serviceName, string serviceType)
    {
        var timeTaken = endTime - startTime;
        var traceData = new ApiTraceData
        {
            RequestTime = DateTime.UtcNow,
            Url = url,
            Method = method,
            Caller = Constants.ServiceType.Self,
            RequestHeaders = HelperFunctions.ToJsonObject(requestHeaders),
            RequestMessage = requestMessage,
            ResponseTime = endTime, 
            TimeTaken = HelperFunctions.TimespanToWords(timeTaken),
            TimeSeconds = timeTaken.TotalSeconds,
            ResponseMessage = responseMessage,
            ResponseHeaders = responseHeaders,
            StatusCode = ((int)statusCode).ToString(),
            StatusCodeDescription = statusCode.ToString(),
            
        };
        
        var status = HelperFunctions.IsServiceUp(statusCode)
            ? Constants.ServiceStatus.Up
            : Constants.ServiceStatus.Down;

        var desc = string.IsNullOrWhiteSpace(serviceDescription)
            ? "Outbound"
            : serviceDescription;

        _logger.LogInformation("{description}, {url}, {serviceName}, {serviceType}, {status}, {traceData}", desc, traceData.Url, serviceName, serviceType, status, traceData);

    }

    private static void GetStatusCode(WebException we, ApiRequestResponse apiRequestResponse)
    {
        if (we.Message.ToLower().Contains("timed out")
            || we.Status is WebExceptionStatus.Timeout or WebExceptionStatus.Pending)
        {
            apiRequestResponse.HttpStatusCode = HttpStatusCode.RequestTimeout;
        }
    }

    protected virtual void ExtractObject<T>(ApiRequestResponse<T> apiRequestResponse, string format)
    {
        try
        {
            if (typeof(T).Name == nameof(Object))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(apiRequestResponse.Response))
            {
                return;
            }

            var content = apiRequestResponse.Response.Trim();

            if (HelperFunctions.IsValidJson(content))
            {
                apiRequestResponse.Result = content.DeserializeFromJson<T>();
            }
            else if (HelperFunctions.IsValidXml(content))
            {
                apiRequestResponse.Result = content.DeserializeFromXml<T>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"While deserializing response: " +
                                $"{apiRequestResponse.Response} from: {format}");
        }
    }

    protected virtual async Task<HttpResponseMessage> MakeHttpCall<T>(ApiRequestDetails apiRequestDetails, HttpClient client, HttpRequestMessage request)
    {
        return await client.SendAsync(request);
    }
    #endregion
}
