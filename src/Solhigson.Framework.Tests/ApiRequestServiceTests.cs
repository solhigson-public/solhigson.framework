using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Solhigson.Framework.Web.Api;
using Xunit;

namespace Solhigson.Framework.Tests;

public class ApiRequestServiceTests
{
    private static TestableApiRequestService CreateService(
        ApiConfiguration? config = null)
    {
        var factory = new StubHttpClientFactory();
        return new TestableApiRequestService(factory, config ?? new ApiConfiguration());
    }

    [Fact]
    public async Task SendAsync_SuccessfulJsonResponse_DeserializesResult()
    {
        var service = CreateService();
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"test\",\"value\":42}", System.Text.Encoding.UTF8, "application/json")
        };

        var result = await service.SendAsync<TestDto>(ApiRequest.Get("https://example.com/api"));

        result.IsSuccessful.ShouldBeTrue();
        result.Result.ShouldNotBeNull();
        result.Result.Name.ShouldBe("test");
        result.Result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task SendAsync_NonGeneric_ReturnsResponseString()
    {
        var service = CreateService();
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":1}", System.Text.Encoding.UTF8, "application/json")
        };

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.IsSuccessful.ShouldBeTrue();
        result.Response.ShouldContain("\"data\"");
    }

    [Fact]
    public async Task SendAsync_ErrorResponse_MapsHttpCallResult()
    {
        var service = CreateService();
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.HttpCallResult.Outcome.ShouldBe(RequestOutcome.HttpError);
        result.HttpStatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendAsync_VendorNetworkLikeStatus_MapsCorrectly()
    {
        var service = CreateService();
        service.CannedResponse = new HttpResponseMessage((HttpStatusCode)522)
        {
            Content = new StringContent("")
        };

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.HttpCallResult.Outcome.ShouldBe(RequestOutcome.VendorNetworkLikeHttpError);
    }

    [Fact]
    public async Task SendAsync_TransientStatus_IsRetryable()
    {
        var service = CreateService();
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("")
        };

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.HttpCallResult.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_OperationCanceledException_MapsToTimeout()
    {
        var service = CreateService();
        service.ExceptionToThrow = new OperationCanceledException("Request timeout");

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.HttpCallResult.Outcome.ShouldBe(RequestOutcome.TransportNetworkError);
        result.HttpStatusCode.ShouldBe(HttpStatusCode.RequestTimeout);
        result.HttpCallResult.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_SocketException_MapsToServiceUnavailable()
    {
        var service = CreateService();
        service.ExceptionToThrow = new HttpRequestException(
            "Connection refused",
            new SocketException((int)SocketError.ConnectionRefused));

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.HttpCallResult.Outcome.ShouldBe(RequestOutcome.TransportNetworkError);
        result.HttpStatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        result.HttpCallResult.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_LogTracePerRequest_OverridesConfig()
    {
        var config = new ApiConfiguration { LogOutBoundApiRequests = false };
        var service = CreateService(config);
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        };

        await service.SendAsync(ApiRequest.Get("https://example.com/api").WithLogTrace());

        service.TraceCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_LogTraceDisabled_SkipsTrace()
    {
        var config = new ApiConfiguration { LogOutBoundApiRequests = false };
        var service = CreateService(config);
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        };

        await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        service.TraceCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task SendAsync_ServiceNameDefaults_ToHost()
    {
        var config = new ApiConfiguration { LogOutBoundApiRequests = true };
        var service = CreateService(config);
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        };

        await service.SendAsync(ApiRequest.Get("https://myhost.example.com/api"));

        service.LastTracedServiceName.ShouldBe("myhost.example.com");
    }

    #region Test Infrastructure

    private class TestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private class TestableApiRequestService(IHttpClientFactory factory, ApiConfiguration config)
        : ApiRequestService(factory, config)
    {
        public HttpResponseMessage? CannedResponse { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public int TraceCallCount { get; private set; }
        public string? LastTracedServiceName { get; private set; }

        protected override Task<HttpResponseMessage> MakeHttpCall<T>(
            ApiRequest apiRequestDetails, HttpClient client, HttpRequestMessage request,
            CancellationToken ct)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(CannedResponse
                ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        }

        protected override void SaveApiTraceData(string url, string method,
            System.Collections.Generic.IReadOnlyDictionary<string, string>? requestHeaders,
            DateTime startTime, DateTime endTime, string? requestMessage, string? responseMessage,
            System.Collections.Generic.Dictionary<string, string>? responseHeaders,
            HttpStatusCode statusCode, string? serviceDescription, string serviceName, string serviceType)
        {
            TraceCallCount++;
            LastTracedServiceName = serviceName;
        }
    }

    #endregion
}
