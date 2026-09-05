using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Inbound half of the <see cref="IApiTraceSink"/> seam. Shares a collection with
/// <see cref="ApiTraceSinkTests"/> because both mutate the process-wide
/// <c>ServiceProviderWrapper.ServiceProvider</c>.
/// </summary>
[Collection(ApiTraceScopedPropertiesCollection.Name)]
public class ApiTraceMiddlewareSinkTests
{
    private const string ResponseBody = "{\"ok\":true}";
    private const string RequestBody = "{\"id\":7}";

    [Fact]
    public async Task InboundRequest_CapturingSink_ReceivesTheInboundPayload()
    {
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext();
        context.Request.Headers[ApiTraceData.UserHttpHeaderIdentifier] = "caller@example.com";

        using var scope = new LogScopedProperties(chainId: "chain-inbound");

        await middleware.InvokeAsync(context, WriteOkResponse);

        var trace = sink.Traces.ShouldHaveSingleItem();

        // The two literals consumers filter on. They must be exactly what the middleware logged as
        // {serviceName} and {serviceType} before the sink seam existed.
        trace.ServiceName.ShouldBe("Self");
        trace.ServiceName.ShouldBe(Constants.ServiceType.Self);
        trace.ServiceType.ShouldBe(Constants.Group.ServiceStatus);

        trace.Direction.ShouldBe(ApiTraceDirection.Inbound);
        trace.ServiceDescription.ShouldBe("Inbound"); // no route action on a bare context
        trace.Status.ShouldBe(Constants.ServiceStatus.Up);
        trace.ChainId.ShouldBe("chain-inbound");
        trace.UserIdentity.ShouldBe("caller@example.com");

        trace.Method.ShouldBe("POST");
        trace.Url.ShouldContain("api/orders");
        trace.RequestMessage.ShouldBe(RequestBody);
        trace.ResponseMessage.ShouldBe(ResponseBody);
        trace.StatusCode.ShouldBe(((int)HttpStatusCode.OK).ToString());
    }

    [Fact]
    public async Task InboundRequest_ServerError_CarriesDownStatus()
    {
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext();

        await middleware.InvokeAsync(context, async c =>
        {
            c.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await c.Response.WriteAsync(ResponseBody);
        });

        sink.Traces.ShouldHaveSingleItem().Status.ShouldBe(Constants.ServiceStatus.Down);
    }

    [Fact]
    public async Task InboundRequest_ThrowingSink_IsSwallowedAndTheResponseStillReachesTheClient()
    {
        var sink = new ThrowingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext();
        var clientStream = new MemoryStream();
        context.Response.Body = clientStream;

        await middleware.InvokeAsync(context, WriteOkResponse);

        sink.Calls.ShouldBe(1);
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(ResponseBody);

        // The NORMAL-path half of the restore. ThrowingNext_RestoresTheOriginalResponseStream_AndRethrows
        // pins only the throwing path, so without this a refactor that moved the restore out of the
        // finally into a catch would stay green while leaving every outer middleware (elfrique's
        // UseStatusCodePages, for one) holding the buffer this method has already disposed.
        context.Response.Body.ShouldBeSameAs(clientStream);
    }

    [Fact]
    public async Task InboundRequest_ChunkedBodyWithoutContentLength_IsTracedInFull()
    {
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext(requestBody: RequestBody, setContentLength: false);

        await middleware.InvokeAsync(context, WriteOkResponse);

        context.Request.ContentLength.ShouldBeNull();
        sink.Traces.ShouldHaveSingleItem().RequestMessage.ShouldBe(RequestBody);
    }

    [Fact]
    public async Task InboundRequest_LargeBody_IsTracedInFull()
    {
        // Larger than the recyclable stream's block size and than the buffering stream's memory
        // threshold, so the body crosses every buffer boundary on the way through. What makes this red
        // against the old code is the same thing as the fact above: no Content-Length on a chunked or
        // streamed request, so the buffer sized from it was empty and the whole body was traced as "".
        var largeBody = "{\"payload\":\"" + new string('x', 512 * 1024) + "\"}";
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext(requestBody: largeBody, setContentLength: false);

        await middleware.InvokeAsync(context, WriteOkResponse);

        var trace = sink.Traces.ShouldHaveSingleItem();
        trace.RequestMessage!.Length.ShouldBe(largeBody.Length);
        trace.RequestMessage.ShouldBe(largeBody);
    }

    [Fact]
    public async Task InboundRequest_BodyIsStillReadableByTheDownstreamHandler()
    {
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext();
        string? seenByHandler = null;

        await middleware.InvokeAsync(context, async c =>
        {
            using (var reader = new StreamReader(c.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                seenByHandler = await reader.ReadToEndAsync();
            }

            await WriteOkResponse(c);
        });

        seenByHandler.ShouldBe(RequestBody);
        sink.Traces.ShouldHaveSingleItem().RequestMessage.ShouldBe(RequestBody);
    }

    [Fact]
    public async Task ThrowingNext_RestoresTheOriginalResponseStream_AndRethrows()
    {
        // Without the finally, context.Response.Body still points at the buffer this method disposes on
        // the way out, so the outer exception handler writes its payload into a disposed stream and the
        // client gets an empty 500.
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext();
        var clientStream = new MemoryStream();
        context.Response.Body = clientStream;
        var boom = new InvalidOperationException("handler blew up");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw boom));

        thrown.ShouldBeSameAs(boom);                      // propagates unchanged
        context.Response.Body.ShouldBeSameAs(clientStream); // and the real stream is back
        sink.Traces.ShouldBeEmpty();                       // no trace for a request that never finished

        // The restored stream is writable and empty, which is what the outer handler needs.
        await context.Response.WriteAsync("{\"error\":\"handled\"}");
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe("{\"error\":\"handled\"}");
    }

    [Fact]
    public void ContainerResolvedMiddleware_GetsTheConsumerSink()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<CapturingSink>().As<IApiTraceSink>().SingleInstance();
        builder.RegisterSolhigsonDependencies(new ConfigurationBuilder().Build());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();

        // Reference equality, not type equality: it is the only thing that distinguishes an injected
        // sink from the constructor parameter's C# default.
        var middleware = scope.Resolve<ApiTraceMiddleware>();
        var field = typeof(ApiTraceMiddleware)
            .GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        field.GetValue(middleware).ShouldBeSameAs(scope.Resolve<IApiTraceSink>());
    }

    [Fact]
    public async Task NonApiUrl_IsNotTraced()
    {
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(sink);
        var context = BuildContext("/home/index");

        await middleware.InvokeAsync(context, WriteOkResponse);

        sink.Traces.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoSinkSupplied_FallsBackToTheDefaultSink_AndStillServesTheRequest()
    {
        var middleware = new ApiTraceMiddleware();
        var context = BuildContext();
        var clientStream = new MemoryStream();
        context.Response.Body = clientStream;

        await middleware.InvokeAsync(context, WriteOkResponse);

        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(ResponseBody);
    }

    #region Test Infrastructure

    private static async Task WriteOkResponse(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync(ResponseBody);
    }

    private static DefaultHttpContext BuildContext(string path = "/api/orders",
        string requestBody = RequestBody, bool setContentLength = true)
    {
        var context = new DefaultHttpContext();
        var body = Encoding.UTF8.GetBytes(requestBody);

        context.Request.Scheme = "https";
        context.Request.Host = new HostString("self.example.com");
        context.Request.Path = path;
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";

        // A chunked or streamed request carries NO Content-Length; setContentLength: false is that case.
        if (setContentLength)
        {
            context.Request.ContentLength = body.Length;
        }

        context.Request.Body = new MemoryStream(body);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class CapturingSink : IApiTraceSink
    {
        public List<ApiTraceData> Traces { get; } = [];

        public void Save(ApiTraceData trace) => Traces.Add(trace);
    }

    private sealed class ThrowingSink : IApiTraceSink
    {
        public int Calls { get; private set; }

        public void Save(ApiTraceData trace)
        {
            Calls++;
            throw new InvalidOperationException("sink is down");
        }
    }

    #endregion
}
