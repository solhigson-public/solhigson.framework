using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Utilities;

namespace Solhigson.Framework.Web.Api;

public class ApiRequestService(IHttpClientFactory httpClientFactory, ApiConfiguration apiConfiguration)
    : IApiRequestService
{
    private readonly LogWrapper _logger = Logging.LogManager.GetLogger("ApiRequestHelper");

    public async Task<ApiRequestResponse> SendAsync(ApiRequest request, CancellationToken ct = default)
    {
        return await SendAsync<object>(request, ct);
    }

    public async Task<ApiRequestResponse<T>> SendAsync<T>(ApiRequest request, CancellationToken ct = default)
    {
        return await SendRequestInternalAsync<T>(request, ct);
    }

    protected async Task<ApiRequestResponse<T>> SendRequestInternalAsync<T>(
        ApiRequest apiRequestDetails, CancellationToken ct)
    {
        var logTrace = apiRequestDetails.LogTrace ?? apiConfiguration.LogOutBoundApiRequests;

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
        var client = httpClientFactory.CreateClient(apiRequestDetails.NamedHttpClient ?? "");
        client.DefaultRequestHeaders.ExpectContinue = apiRequestDetails.ExpectContinue;
        using var request = new HttpRequestMessage();

        CancellationTokenSource? linkedCts = null;
        var effectiveCt = ct;

        try
        {
            if (timeOut > 0)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(timeOut));
                effectiveCt = linkedCts.Token;
            }

            request.Method = method;
            request.RequestUri = apiRequestDetails.Uri;
            if (apiRequestDetails.HttpContent is not null)
            {
                request.Content = apiRequestDetails.HttpContent;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    request.Content = new StringContent(data, Encoding.UTF8, format);
                }
                else if (method == HttpMethod.Delete || method == HttpMethod.Put)
                {
                    request.Content ??= new StringContent("", Encoding.UTF8, format);
                }
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
                ? await request.Content.ReadAsStringAsync(effectiveCt)
                : data;

            apiRequestHelperResponse.ResponseHeaders = new Dictionary<string, string>();

            apiRequestHelperResponse.StartTime = DateTime.UtcNow;
            apiRequestHelperResponse.HttpResponseMessage =
                await MakeHttpCall<T>(apiRequestDetails, client, request, effectiveCt);
            apiRequestHelperResponse.HttpCallResult = GetHttpCallResult(apiRequestHelperResponse.HttpResponseMessage);
            apiRequestHelperResponse.EndTime = DateTime.UtcNow;
            if (apiRequestDetails.ReadResponseContent)
            {
                apiRequestHelperResponse.Response =
                    await apiRequestHelperResponse.HttpResponseMessage.Content.ReadAsStringAsync(effectiveCt);
            }
        }
        catch (Exception e)
        {
            OnAnyException(apiRequestDetails, apiRequestHelperResponse, e);

            apiRequestHelperResponse.HttpCallResult = MapToResult(e);
        }
        finally
        {
            try
            {
                apiRequestHelperResponse.EndTime ??= DateTime.UtcNow;
                apiRequestHelperResponse.TimeTaken =
                    apiRequestHelperResponse.EndTime.Value - apiRequestHelperResponse.StartTime;

                var responseFormat = format;
                Dictionary<string, string>? responseHeaders = null;
                if (apiRequestHelperResponse.HttpResponseMessage != null)
                {
                    responseHeaders =
                        HelperFunctions.ToJsonObject(apiRequestHelperResponse.HttpResponseMessage.Headers);
                    if (responseHeaders?.TryGetValue("Content-Type", out var header) == true)
                    {
                        responseFormat = header.ToString();
                    }

                    if (apiRequestHelperResponse.HttpResponseMessage.Headers.HasData())
                    {
                        foreach (var (key, value) in apiRequestHelperResponse.HttpResponseMessage.Headers)
                        {
                            apiRequestHelperResponse.ResponseHeaders?.Add(key, string.Join(",", value));
                        }
                    }

                    if (apiRequestHelperResponse.HttpResponseMessage.RequestMessage?.Headers != null)
                    {
                        apiRequestHelperResponse.RequestHeaders = new Dictionary<string, string>();
                        foreach (var (key, value) in apiRequestHelperResponse.HttpResponseMessage.RequestMessage
                                     .Headers)
                        {
                            apiRequestHelperResponse.RequestHeaders.Add(key, string.Join(",", value));
                        }
                    }
                }

                ExtractObject(apiRequestHelperResponse, responseFormat);

                if (logTrace)
                {
                    var requestHeaders = apiRequestHelperResponse.RequestHeaders ?? apiRequestDetails.Headers;
                    SaveApiTraceData(url, method.ToString(), requestHeaders,
                        apiRequestHelperResponse.StartTime,
                        apiRequestHelperResponse.EndTime ?? DateTime.UtcNow,
                        apiRequestHelperResponse.Request,
                        apiRequestHelperResponse.Response, responseHeaders,
                        apiRequestHelperResponse.HttpStatusCode,
                        apiRequestDetails.ServiceDescription, serviceName, serviceType);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }

            linkedCts?.Dispose();
        }

        return apiRequestHelperResponse;
    }

    protected virtual void SaveApiTraceData(string url, string method,
        IReadOnlyDictionary<string, string>? requestHeaders, DateTime startTime, DateTime endTime,
        string? requestMessage,
        string? responseMessage, Dictionary<string, string>? responseHeaders, HttpStatusCode statusCode,
        string? serviceDescription, string serviceName, string serviceType)
    {
        var timeTaken = endTime - startTime;
        var traceData = new ApiTraceData
        {
            RequestTime = DateTime.UtcNow,
            Url = url,
            Method = method,
            Caller = Constants.ServiceType.Self,
            RequestHeaders = HelperFunctions.ToJsonObject(requestHeaders!),
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

        _logger.LogInformation("{description}, {url}, {serviceName}, {serviceType}, {status}, {traceData}", desc,
            traceData.Url, serviceName, serviceType, status, traceData);
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
            _logger.LogError(e, "While deserializing response: {Response} from: {Format}",
                apiRequestResponse.Response, format);
        }
    }

    protected virtual async Task<HttpResponseMessage> MakeHttpCall<T>(ApiRequest apiRequestDetails,
        HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        return await client.SendAsync(request, ct);
    }

    private void OnAnyException(ApiRequest apiRequestDetails, ApiRequestResponse apiRequestHelperResponse,
        Exception e)
    {
        apiRequestHelperResponse.Response = e.Message;
        _logger.LogError(e, "While sending request to url: {url}", apiRequestDetails.Uri);
        apiRequestHelperResponse.HttpStatusDescription = e.Message;
    }

    private static HttpCallResult GetHttpCallResult(HttpResponseMessage resp)
    {
        if (IsVendorNetworkLike(resp.StatusCode))
        {
            return HttpCallResult.New(
                RequestOutcome.VendorNetworkLikeHttpError,
                resp.StatusCode,
                resp.ReasonPhrase,
                IsRetryableVendorNetworkLike(resp.StatusCode) || IsPotentiallyTransient(resp));
        }

        if (!resp.IsSuccessStatusCode)
        {
            return HttpCallResult.New(
                RequestOutcome.HttpError,
                resp.StatusCode,
                resp.ReasonPhrase,
                IsPotentiallyTransient(resp));
        }

        return HttpCallResult.New(RequestOutcome.Success, resp.StatusCode, resp.ReasonPhrase);
    }

    private static bool IsPotentiallyTransient(HttpResponseMessage resp)
    {
        var code = (int)resp.StatusCode;
        if (resp.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout)
            return true;
        if (resp.Headers.RetryAfter != null) return true;
        if (resp.StatusCode == (HttpStatusCode)429) return true;
        return code is >= 500 and <= 599;
    }

    private static bool IsVendorNetworkLike(HttpStatusCode code)
    {
        var n = (int)code;
        return n is 444 or 495 or 496 or 497 or 499
            or 522 or 523 or 524 or 525 or 526 or 527
            or 529 or 598 or 599;
    }

    private static bool IsRetryableVendorNetworkLike(HttpStatusCode code)
    {
        var n = (int)code;
        return n is 444 or 499 or 522 or 523 or 524 or 527 or 529 or 598 or 599;
    }

    private static HttpCallResult MapToResult(Exception ex)
        => ex switch
        {
            OperationCanceledException
                => HttpCallResult.New(RequestOutcome.TransportNetworkError, HttpStatusCode.RequestTimeout,
                    "Request timeout", true, ex.GetType().FullName),

            HttpRequestException { InnerException: SocketException se }
                => HttpCallResult.New(RequestOutcome.TransportNetworkError, HttpStatusCode.ServiceUnavailable,
                    se.SocketErrorCode.ToString(), true, ex.GetType().FullName),

            AuthenticationException aex
                => HttpCallResult.New(RequestOutcome.TransportNetworkError, 0, $"TLS: {aex.Message}",
                    errorType: ex.GetType().FullName),

            HttpRequestException hrex
                => HttpCallResult.New(RequestOutcome.TransportNetworkError, 0, hrex.Message, true,
                    ex.GetType().FullName),

            _ => HttpCallResult.New(RequestOutcome.TransportNetworkError, 0, ex.Message,
                errorType: ex.GetType().FullName)
        };
}
