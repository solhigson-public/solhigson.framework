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
    private readonly LogWrapper _logger = Logging.LogManager.GetLogger(nameof(ApiRequestService));

    public async Task<ApiRequestResponse> SendAsync(ApiRequest request, CancellationToken ct = default)
    {
        return await SendAsync<object>(request, ct);
    }

    public async Task<ApiRequestResponse<T>> SendAsync<T>(ApiRequest request, CancellationToken ct = default)
    {
        return await SendRequestInternalAsync<T>(request, ct);
    }

    protected async Task<ApiRequestResponse<T>> SendRequestInternalAsync<T>(
        ApiRequest apiRequest, CancellationToken ct)
    {
        var logTrace = apiRequest.LogTrace ?? apiConfiguration.LogOutBoundApiRequests;

        var response = new ApiRequestResponse<T>();
        var client = httpClientFactory.CreateClient(apiRequest.NamedHttpClient ?? "");
        using var request = new HttpRequestMessage();
        request.Headers.ExpectContinue = apiRequest.ExpectContinue;

        CancellationTokenSource? linkedCts = null;
        var effectiveCt = ct;

        try
        {
            if (apiRequest.TimeOut > 0)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(apiRequest.TimeOut));
                effectiveCt = linkedCts.Token;
            }

            request.Method = apiRequest.HttpMethod;
            request.RequestUri = apiRequest.Uri;
            if (apiRequest.HttpContent is not null)
            {
                request.Content = apiRequest.HttpContent;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(apiRequest.Payload))
                {
                    request.Content = new StringContent(apiRequest.Payload, Encoding.UTF8, apiRequest.Format);
                }
                else if (apiRequest.HttpMethod == HttpMethod.Put)
                {
                    request.Content = new StringContent("", Encoding.UTF8, apiRequest.Format);
                }
            }

            if (apiRequest.Headers is { Count: > 0 })
            {
                foreach (var key in apiRequest.Headers.Keys)
                {
                    if (apiRequest.Headers.TryGetValue(key, out var value) &&
                        !string.IsNullOrWhiteSpace(value))
                    {
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }

            response.Request = apiRequest.Payload;

            response.StartTime = DateTime.UtcNow;
            response.HttpResponseMessage =
                await MakeHttpCall<T>(apiRequest, client, request, effectiveCt);
            response.HttpCallResult = GetHttpCallResult(response.HttpResponseMessage);
            response.EndTime = DateTime.UtcNow;
            if (apiRequest.ReadResponseContent)
            {
                response.Response =
                    await response.HttpResponseMessage.Content.ReadAsStringAsync(effectiveCt);
            }
        }
        catch (Exception e)
        {
            OnAnyException(apiRequest, response, e);
            response.HttpCallResult = MapToResult(e, ct);
        }
        finally
        {
            try
            {
                response.EndTime ??= DateTime.UtcNow;
                response.TimeTaken = response.EndTime.Value - response.StartTime;

                var responseFormat = ContentTypes.Json;
                if (response.HttpResponseMessage is not null)
                {
                    if (response.HttpResponseMessage.Content.Headers.ContentType?.MediaType is { } mediaType)
                    {
                        responseFormat = mediaType;
                    }

                    if (response.HttpResponseMessage.Headers.HasData())
                    {
                        response.ResponseHeaders = new Dictionary<string, string>();
                        foreach (var (key, value) in response.HttpResponseMessage.Headers)
                        {
                            response.ResponseHeaders.Add(key, string.Join(",", value));
                        }
                    }

                    if (response.HttpResponseMessage.RequestMessage?.Headers is { } reqHeaders)
                    {
                        response.RequestHeaders = new Dictionary<string, string>();
                        foreach (var (key, value) in reqHeaders)
                        {
                            response.RequestHeaders.Add(key, string.Join(",", value));
                        }
                    }
                }

                ExtractObject(response, responseFormat);

                if (logTrace)
                {
                    SaveApiTraceData(response, apiRequest);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }

            linkedCts?.Dispose();
        }

        return response;
    }

    protected virtual void SaveApiTraceData(ApiRequestResponse response, ApiRequest apiRequest)
    {
        var serviceName = string.IsNullOrWhiteSpace(apiRequest.ServiceName)
            ? apiRequest.Uri.Host
            : apiRequest.ServiceName;

        var serviceType = string.IsNullOrWhiteSpace(apiRequest.ServiceType)
            ? Constants.ServiceType.External
            : apiRequest.ServiceType;

        var requestHeaders = response.RequestHeaders ?? apiRequest.Headers;
        var responseHeaders = response.HttpResponseMessage is not null
            ? HelperFunctions.ToJsonObject(response.HttpResponseMessage.Headers)
            : null;

        var traceData = new ApiTraceData
        {
            RequestTime = response.StartTime,
            Url = apiRequest.Uri.ToString(),
            Method = apiRequest.HttpMethod.ToString(),
            Caller = Constants.ServiceType.Self,
            RequestHeaders = HelperFunctions.ToJsonObject(requestHeaders!),
            RequestMessage = response.Request,
            ResponseTime = response.EndTime ?? DateTime.UtcNow,
            TimeTaken = HelperFunctions.TimespanToWords(response.TimeTaken),
            TimeSeconds = response.TimeTaken.TotalSeconds,
            ResponseMessage = response.Response,
            ResponseHeaders = responseHeaders,
            StatusCode = ((int)response.HttpStatusCode).ToString(),
            StatusCodeDescription = response.HttpStatusCode.ToString(),
        };

        var status = HelperFunctions.IsServiceUp(response.HttpStatusCode)
            ? Constants.ServiceStatus.Up
            : Constants.ServiceStatus.Down;

        var desc = string.IsNullOrWhiteSpace(apiRequest.ServiceDescription)
            ? "Outbound"
            : apiRequest.ServiceDescription;

        _logger.LogInformation("{description}, {url}, {serviceName}, {serviceType}, {status}, {traceData}", desc,
            traceData.Url, serviceName, serviceType, status, traceData);
    }

    protected virtual void ExtractObject<T>(ApiRequestResponse<T> apiRequestResponse, string format)
    {
        try
        {
            if (typeof(T) == typeof(object))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(apiRequestResponse.Response))
            {
                return;
            }

            var content = apiRequestResponse.Response.Trim();

            if (format.Contains("json", StringComparison.OrdinalIgnoreCase)
                || HelperFunctions.IsValidJson(content))
            {
                apiRequestResponse.Result = content.DeserializeFromJson<T>();
            }
            else if (format.Contains("xml", StringComparison.OrdinalIgnoreCase)
                     || HelperFunctions.IsValidXml(content))
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

    protected virtual async Task<HttpResponseMessage> MakeHttpCall<T>(ApiRequest apiRequest,
        HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        return await client.SendAsync(request, ct);
    }

    private void OnAnyException(ApiRequest apiRequest, ApiRequestResponse response, Exception e)
    {
        response.Response ??= e.Message;
        _logger.LogError(e, "While sending request to url: {url}", apiRequest.Uri);
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
        if (resp.StatusCode is HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.RequestTimeout)
            return true;
        if (resp.Headers.RetryAfter is not null) return true;
        return resp.StatusCode == (HttpStatusCode)429;
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

    private static HttpCallResult MapToResult(Exception ex, CancellationToken callerCt)
        => ex switch
        {
            OperationCanceledException when callerCt.IsCancellationRequested
                => HttpCallResult.New(RequestOutcome.TransportNetworkError, 0,
                    "Request cancelled by caller", errorType: ex.GetType().FullName),

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
