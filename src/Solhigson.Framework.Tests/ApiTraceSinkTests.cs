using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Web.Api;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Outbound half of the <see cref="IApiTraceSink"/> seam, plus the default sink's emission and the
/// container registration. The inbound half lives in <see cref="ApiTraceMiddlewareSinkTests"/>.
/// </summary>
/// <remarks>
/// Both classes share the <see cref="ApiTraceScopedPropertiesCollection"/> collection because both
/// mutate the process-wide <c>ServiceProviderWrapper.ServiceProvider</c> to make the log-scoped
/// chain id observable; the collection serialises them so neither can clear the other's provider.
/// </remarks>
[Collection(ApiTraceScopedPropertiesCollection.Name)]
public class ApiTraceSinkTests
{
    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task OutboundTrace_CapturingSink_ReceivesEveryWidenedField()
    {
        var sink = new CapturingApiTraceSink();
        var service = CreateService(sink, new ApiConfiguration { LogOutBoundApiRequests = true });
        service.ExceptionToThrow = new HttpRequestException(
            "Connection refused",
            new SocketException((int)SocketError.ConnectionRefused));

        using var scope = new LogScopedProperties(chainId: "chain-42", userEmail: "caller@example.com");

        await service.SendAsync(ApiRequest.Get("https://vendor.example.com/api/lookup")
            .WithHeader("x-test", "1")
            .WithServiceName("vendor-x")
            .WithServiceType(Constants.ServiceType.Internal)
            .WithServiceDescription("Vendor Lookup"));

        var trace = sink.Traces.ShouldHaveSingleItem();

        // Every field the seam widened ApiTraceData with, one assertion each.
        trace.ServiceName.ShouldBe("vendor-x");
        trace.ServiceType.ShouldBe(Constants.ServiceType.Internal);
        trace.ServiceDescription.ShouldBe("Vendor Lookup");
        trace.Status.ShouldBe(Constants.ServiceStatus.Down);
        trace.Direction.ShouldBe(ApiTraceDirection.Outbound);
        trace.ChainId.ShouldBe("chain-42");
        trace.UserIdentity.ShouldBe("caller@example.com");
        trace.ExceptionType.ShouldBe(typeof(HttpRequestException).FullName);
        trace.ExceptionMessage.ShouldBe(nameof(SocketError.ConnectionRefused));

        // and the pre-existing payload is still filled in exactly as before.
        trace.Url.ShouldBe("https://vendor.example.com/api/lookup");
        trace.Method.ShouldBe("GET");
        trace.Caller.ShouldBe(Constants.ServiceType.Self);
        trace.StatusCode.ShouldBe(((int)HttpStatusCode.ServiceUnavailable).ToString());
        trace.RequestHeaders["x-test"].ShouldBe("1");
    }

    [Fact]
    public async Task OutboundTrace_SuccessfulCall_DefaultsServiceFieldsAndCarriesNoException()
    {
        var sink = new CapturingApiTraceSink();
        var service = CreateService(sink, new ApiConfiguration { LogOutBoundApiRequests = true });
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        };

        await service.SendAsync(ApiRequest.Get("https://myhost.example.com/api"));

        var trace = sink.Traces.ShouldHaveSingleItem();
        trace.ServiceName.ShouldBe("myhost.example.com");          // defaults to the host
        trace.ServiceType.ShouldBe(Constants.ServiceType.External); // defaults to External
        trace.ServiceDescription.ShouldBe("Outbound");              // defaults to "Outbound"
        trace.Status.ShouldBe(Constants.ServiceStatus.Up);
        trace.Direction.ShouldBe(ApiTraceDirection.Outbound);
        trace.ExceptionType.ShouldBeNull();

        // HttpCallResult.Reason is the HTTP reason PHRASE ("OK") on a successful call, not an
        // exception message; copying it verbatim would make every 200 look like it carried one.
        trace.ExceptionMessage.ShouldBeNull();

        trace.ChainId.ShouldBeNull();      // no log scope registered outside a request
        trace.UserIdentity.ShouldBeNull();
    }

    [Fact]
    public async Task OutboundTrace_HttpErrorResponse_IsUpWithNoExceptionFields()
    {
        var sink = new CapturingApiTraceSink();
        var service = CreateService(sink, new ApiConfiguration { LogOutBoundApiRequests = true });
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
            Content = new StringContent("missing")
        };

        await service.SendAsync(ApiRequest.Get("https://example.com/api/missing"));

        var trace = sink.Traces.ShouldHaveSingleItem();
        trace.StatusCode.ShouldBe(((int)HttpStatusCode.NotFound).ToString());

        // Up, NOT Down: HelperFunctions.IsServiceUp is (int)statusCode < 500, so a 404 means the
        // service answered. Only 5xx and the transport failures below are Down (see
        // OutboundTrace_CapturingSink_ReceivesEveryWidenedField, which is a 503).
        trace.Status.ShouldBe(Constants.ServiceStatus.Up);

        // A 404 is a transport SUCCESS with an unhappy status: the service answered, so no exception.
        trace.ExceptionType.ShouldBeNull();
        trace.ExceptionMessage.ShouldBeNull();
    }

    [Fact]
    public async Task OutboundTrace_ThrowingSink_IsSwallowedAndResultStillReturns()
    {
        var sink = new ThrowingApiTraceSink();
        var service = CreateService(sink, new ApiConfiguration { LogOutBoundApiRequests = true });
        service.CannedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"test\",\"value\":42}", System.Text.Encoding.UTF8,
                "application/json")
        };

        var result = await service.SendAsync<TestDto>(ApiRequest.Get("https://example.com/api"));

        sink.Calls.ShouldBe(1);
        result.IsSuccessful.ShouldBeTrue();
        result.Result.ShouldNotBeNull();
        result.Result.Value.ShouldBe(42);
    }

    [Fact]
    public void OutboundTrace_ThrowingSink_IsSwallowedInsideSaveApiTraceDataItself()
    {
        // SendRequestInternalAsync's finally already has a catch-all, so the previous fact cannot tell
        // whether SaveApiTraceData swallows on its own. Call it directly: a subclass that overrides
        // nothing (elfrique's decorator shape) must never see the sink's exception.
        var sink = new ThrowingApiTraceSink();
        var service = CreateService(sink, new ApiConfiguration { LogOutBoundApiRequests = true });
        var response = new ApiRequestResponse
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            HttpCallResult = HttpCallResult.New(RequestOutcome.Success, HttpStatusCode.OK)
        };

        Should.NotThrow(() =>
            service.InvokeSaveApiTraceData(response, ApiRequest.Get("https://example.com/api")));

        sink.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task TwoArgumentConstruction_Compiles_AndTracesThroughTheDefaultSink()
    {
        // The consumer constructs the service with two arguments (elfrique's
        // HotelBookingPersistCompensationTests does exactly this), so the sink parameter must stay
        // optional. This line failing to compile IS the regression.
        var direct = new ApiRequestService(new StubHttpClientFactory(), new ApiConfiguration());
        direct.ShouldNotBeNull();

        // ...and the same two-argument shape through a subclass, which is how this repo's own
        // TestableApiRequestService calls the base constructor. Tracing is ON, so the null sink has to
        // fall back to LoggingApiTraceSink; if it did not, this would NullReference through the finally.
        var service = new TwoArgumentApiRequestService(new StubHttpClientFactory(),
            new ApiConfiguration { LogOutBoundApiRequests = true })
        {
            CannedResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }
        };

        var result = await service.SendAsync(ApiRequest.Get("https://example.com/api"));

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DefaultSink_OutboundDirection_EmitsTodaysLoggerNameTemplateLevelAndSixArguments()
    {
        var factory = new CapturingLoggerFactory();
        var sink = new LoggingApiTraceSink(factory);
        var trace = new ApiTraceData
        {
            Url = "https://vendor.example.com/api/lookup",
            ServiceName = "vendor-x",
            ServiceType = Constants.ServiceType.External,
            ServiceDescription = "Vendor Lookup",
            Status = Constants.ServiceStatus.Up,
            Direction = ApiTraceDirection.Outbound
        };

        sink.Save(trace);

        var entry = factory.Entries.ShouldHaveSingleItem();

        // The logger name is persisted downstream as AppLog.Logger and consumers query on it.
        entry.LoggerName.ShouldBe("ApiRequestService");
        entry.Level.ShouldBe(LogLevel.Information);

        // Literal, not the const: a change to the const must fail here.
        entry.Template.ShouldBe("{description}, {url}, {serviceName}, {serviceType}, {status}, {traceData}");

        entry.ArgumentNames.ShouldBe(
            new[] { "description", "url", "serviceName", "serviceType", "status", "traceData" });
        entry.ArgumentValues[0].ShouldBe("Vendor Lookup");
        entry.ArgumentValues[1].ShouldBe("https://vendor.example.com/api/lookup");
        entry.ArgumentValues[2].ShouldBe("vendor-x");
        entry.ArgumentValues[3].ShouldBe(Constants.ServiceType.External);
        entry.ArgumentValues[4].ShouldBe(Constants.ServiceStatus.Up);
        entry.ArgumentValues[5].ShouldBeSameAs(trace);
    }

    [Fact]
    public void DefaultSink_InboundDirection_EmitsUnderTheMiddlewareLoggerName()
    {
        var factory = new CapturingLoggerFactory();
        var sink = new LoggingApiTraceSink(factory);
        var trace = new ApiTraceData
        {
            Url = "https://self.example.com/api/orders",
            ServiceName = Constants.ServiceType.Self,
            ServiceType = Constants.Group.ServiceStatus,
            ServiceDescription = "Inbound",
            Status = Constants.ServiceStatus.Up,
            Direction = ApiTraceDirection.Inbound
        };

        sink.Save(trace);

        var entry = factory.Entries.ShouldHaveSingleItem();
        entry.LoggerName.ShouldBe("ApiTraceMiddleware");
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Template.ShouldBe("{description}, {url}, {serviceName}, {serviceType}, {status}, {traceData}");
        entry.ArgumentNames.ShouldBe(
            new[] { "description", "url", "serviceName", "serviceType", "status", "traceData" });
        entry.ArgumentValues[0].ShouldBe("Inbound");
        entry.ArgumentValues[1].ShouldBe("https://self.example.com/api/orders");
        entry.ArgumentValues[2].ShouldBe(Constants.ServiceType.Self);
        entry.ArgumentValues[3].ShouldBe(Constants.Group.ServiceStatus);
        entry.ArgumentValues[4].ShouldBe(Constants.ServiceStatus.Up);
        entry.ArgumentValues[5].ShouldBeSameAs(trace);
    }

    [Fact]
    public void DefaultSink_UsesOneLoggerPerDirection_NotOnePerCall()
    {
        var factory = new CapturingLoggerFactory();
        var sink = new LoggingApiTraceSink(factory);

        sink.Save(new ApiTraceData { Direction = ApiTraceDirection.Outbound });
        sink.Save(new ApiTraceData { Direction = ApiTraceDirection.Inbound });

        factory.CreatedLoggerNames.ShouldBe(new[] { "ApiRequestService", "ApiTraceMiddleware" });
        factory.Entries.Select(e => e.LoggerName)
            .ShouldBe(new[] { "ApiRequestService", "ApiTraceMiddleware" });
    }

    [Fact]
    public void DefaultSink_PublicConstructor_HoldsTheLogManagerCachedWrappersForBothNames()
    {
        // The facts above capture through the internal ILoggerFactory constructor; this one pins the
        // PUBLIC constructor — the one production uses — to the same LogManager-cached LogWrapper
        // instances the two call sites held before the seam. LogManager caches one wrapper per name,
        // so reference equality is the assertion.
        var sink = new LoggingApiTraceSink();

        FieldValue(sink, "_outboundLogger").ShouldBeSameAs(LogManager.GetLogger("ApiRequestService"));
        FieldValue(sink, "_inboundLogger").ShouldBeSameAs(LogManager.GetLogger("ApiTraceMiddleware"));
    }

    [Fact]
    public void NoConsumerRegistration_ResolvesLoggingApiTraceSinkDefault()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IApiTraceSink>().ShouldBeOfType<LoggingApiTraceSink>();
    }

    [Fact]
    public void ConsumerRegistration_OverridesDefaultSink_PreserveExistingDefaults()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<CapturingApiTraceSink>().As<IApiTraceSink>().SingleInstance();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IApiTraceSink>().ShouldBeOfType<CapturingApiTraceSink>();
    }

    [Fact]
    public void ContainerResolvedApiRequestService_GetsTheConsumerSink()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<CapturingApiTraceSink>().As<IApiTraceSink>().SingleInstance();
        builder.RegisterType<StubHttpClientFactory>().As<IHttpClientFactory>().SingleInstance();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();

        // Autofac must fill the OPTIONAL constructor parameter from the registration rather than fall
        // back to its default value; if it fell back, the consumer's sink would silently never be
        // called. Only the private field can tell the two apart, hence the reflection.
        var service = scope.Resolve<IApiRequestService>().ShouldBeOfType<ApiRequestService>();
        var field = typeof(ApiRequestService)
            .GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        field.GetValue(service).ShouldBeSameAs(scope.Resolve<IApiTraceSink>());
        scope.Resolve<IApiTraceSink>().ShouldBeOfType<CapturingApiTraceSink>();
    }

    #region Test Infrastructure

    private static TracingApiRequestService CreateService(IApiTraceSink sink, ApiConfiguration config)
        => new(new StubHttpClientFactory(), config, sink);

    private static object? FieldValue(object instance, string fieldName)
    {
        var field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        return field.GetValue(instance);
    }

    private class TestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    internal sealed class CapturingApiTraceSink : IApiTraceSink
    {
        public List<ApiTraceData> Traces { get; } = [];

        public void Save(ApiTraceData trace) => Traces.Add(trace);
    }

    private sealed class ThrowingApiTraceSink : IApiTraceSink
    {
        public int Calls { get; private set; }

        public void Save(ApiTraceData trace)
        {
            Calls++;
            throw new InvalidOperationException("sink is down");
        }
    }

    internal sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private class TracingApiRequestService(
        IHttpClientFactory factory, ApiConfiguration config, IApiTraceSink? sink)
        : ApiRequestService(factory, config, sink)
    {
        public HttpResponseMessage? CannedResponse { get; set; }
        public Exception? ExceptionToThrow { get; set; }

        public void InvokeSaveApiTraceData(ApiRequestResponse response, ApiRequest apiRequest)
            => SaveApiTraceData(response, apiRequest);

        protected override Task<HttpResponseMessage> MakeHttpCall<T>(ApiRequest apiRequest,
            HttpClient client, HttpRequestMessage request, CancellationToken ct)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(CannedResponse
                ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        }
    }

    /// <summary>Subclass whose base call passes only two arguments — the shape consumers use.</summary>
    private sealed class TwoArgumentApiRequestService(IHttpClientFactory factory, ApiConfiguration config)
        : TracingApiRequestService(factory, config, null);

    #endregion
}

/// <summary>
/// Installs a log scope (chain id + user email) on the process-wide
/// <c>ServiceProviderWrapper.ServiceProvider</c> and tears it down again, so a test can observe the
/// values <c>SaveApiTraceData</c> reads from it. Only the classes in
/// <see cref="ApiTraceScopedPropertiesCollection"/> touch that static, and the collection serialises them.
/// </summary>
internal sealed class LogScopedProperties : IDisposable
{
    private readonly ServiceProvider _provider;

    internal LogScopedProperties(string? chainId = null, string? userEmail = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CurrentLogScopedPropertiesAccessor>();
        _provider = services.BuildServiceProvider();
        ServiceProviderWrapper.ServiceProvider = _provider;

        if (chainId is not null)
        {
            ServiceProviderWrapper.SetCurrentLogChainId(chainId);
        }

        if (userEmail is not null)
        {
            ServiceProviderWrapper.SetCurrentLogUserEmail(userEmail);
        }
    }

    public void Dispose()
    {
        ServiceProviderWrapper.ServiceProvider = null;
        _provider.Dispose();
    }
}

/// <summary>Records every emission an <see cref="ILoggerFactory"/> hands out, by logger name.</summary>
internal sealed class CapturingLoggerFactory : ILoggerFactory
{
    public List<CapturedLogEntry> Entries { get; } = [];
    public List<string> CreatedLoggerNames { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
        CreatedLoggerNames.Add(categoryName);
        return new CapturingLogger(categoryName, Entries);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string name, List<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // ILogger's state for a templated message is FormattedLogValues: the named arguments in
            // template order, followed by {OriginalFormat} carrying the template itself.
            var values = new List<KeyValuePair<string, object?>>();
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                values.AddRange(pairs);
            }

            entries.Add(new CapturedLogEntry(name, logLevel, values, exception));
        }
    }
}

internal sealed record CapturedLogEntry(
    string LoggerName,
    LogLevel Level,
    IReadOnlyList<KeyValuePair<string, object?>> Values,
    Exception? Exception)
{
    private const string OriginalFormat = "{OriginalFormat}";

    public string? Template =>
        Values.FirstOrDefault(v => v.Key == OriginalFormat).Value?.ToString();

    public IReadOnlyList<string> ArgumentNames =>
        Values.Where(v => v.Key != OriginalFormat).Select(v => v.Key).ToList();

    public IReadOnlyList<object?> ArgumentValues =>
        Values.Where(v => v.Key != OriginalFormat).Select(v => v.Value).ToList();
}

[CollectionDefinition(Name, DisableParallelization = true)]
public class ApiTraceScopedPropertiesCollection
{
    public const string Name = "ApiTraceScopedProperties";
}
