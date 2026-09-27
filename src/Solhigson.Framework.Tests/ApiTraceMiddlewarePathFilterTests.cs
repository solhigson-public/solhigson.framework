#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Which inbound requests <see cref="ApiTraceMiddleware"/> traces: the switch, the include list and
/// the exclude list on <see cref="ApiConfiguration"/>. Every negative has a positive control on the
/// same route, so a test cannot go green because nothing is ever traced.
/// </summary>
[Collection(ApiTraceScopedPropertiesCollection.Name)]
public class ApiTraceMiddlewarePathFilterTests
{
    private const string ResponseBody = "{\"ok\":true}";
    private const string LiveResults = "/api/contests/5/live-results";

    [Fact]
    public async Task NoIncludeList_TracesNothing_AndBuffersNothing()
    {
        // Switch on, include function null.
        var noList = new ApiConfiguration { InboundTraceEnabled = () => true };
        var (traced, context, requestBody, clientStream) = await InvokeWithStreams(noList, "/api/orders");

        traced.ShouldBeFalse();
        // No body buffering: the request body is non-seekable, so EnableBuffering would have wrapped
        // it in a buffering stream and the middleware would have read it; neither happened. The
        // response stream the handler saw was the client's own (checked in InvokeWithStreams).
        context.Request.Body.ShouldBeSameAs(requestBody);
        requestBody.Reads.ShouldBe(0);
        context.Response.Body.ShouldBeSameAs(clientStream);
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(ResponseBody);

        // Positive control for the stream assertions, same route: the traced path does read and
        // buffer the same kind of body.
        var (tracedControl, controlContext, controlBody, _) =
            await InvokeWithStreams(Config(include: ["/api/orders"]), "/api/orders");
        tracedControl.ShouldBeTrue();
        controlBody.Reads.ShouldBeGreaterThan(0);
        controlContext.Request.Body.ShouldNotBeSameAs(controlBody);

        // Empty list: the same.
        (await Traces(Config(include: []), "/api/orders")).ShouldBeFalse();

        // Blank entries are ignored, never read as "match everything".
        (await Traces(Config(include: ["", "  "]), "/api/orders")).ShouldBeFalse();

        // Positive control on the same route.
        (await Traces(Config(include: ["/api/orders"]), "/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public async Task IncludeListWithoutApi_DoesNotTraceAnApiUrl_NoApiFallback()
    {
        // Before this change every url containing "api/" was traced. With an include list that does
        // not name /api, an /api url must not be traced.
        (await Traces(Config(include: ["/home"]), "/api/orders")).ShouldBeFalse();

        // Positive control on the same route.
        (await Traces(Config(include: ["/api"]), "/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public async Task ApiPattern_CoversItsSubtree_ButNotAPathThatOnlySharesAPrefix()
    {
        var api = Config(include: ["/api"]);

        (await Traces(api, LiveResults)).ShouldBeTrue();
        (await Traces(api, "/api")).ShouldBeTrue();
        (await Traces(api, "/apiary")).ShouldBeFalse();
        (await Traces(api, "/apiary/api")).ShouldBeFalse();

        // Positive controls on the same routes.
        (await Traces(Config(include: ["/apiary"]), "/apiary")).ShouldBeTrue();
        (await Traces(Config(include: ["/apiary"]), "/apiary/api")).ShouldBeTrue();
    }

    [Fact]
    public async Task PlaceholderSegment_MatchesExactlyOneSegment()
    {
        var pattern = Config(include: ["/api/contests/{id}/live-results"]);

        (await Traces(pattern, LiveResults)).ShouldBeTrue();
        (await Traces(pattern, "/api/contests/abc-9/live-results")).ShouldBeTrue();
        (await Traces(pattern, "/api/contests/5/live-results/stream")).ShouldBeTrue(); // beneath it

        // Two segments where the placeholder is, and none.
        (await Traces(pattern, "/api/contests/5/6/live-results")).ShouldBeFalse();
        (await Traces(pattern, "/api/contests/live-results")).ShouldBeFalse();

        // Positive controls on those same routes.
        (await Traces(Config(include: ["/api/contests/{a}/{b}/live-results"]), "/api/contests/5/6/live-results"))
            .ShouldBeTrue();
        (await Traces(Config(include: ["/api/contests/live-results"]), "/api/contests/live-results"))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task ExcludeBeatsInclude()
    {
        var both = Config(include: ["/api"], exclude: ["/api/contests/{id}/live-results"]);

        (await Traces(both, LiveResults)).ShouldBeFalse();
        (await Traces(both, "/api/orders")).ShouldBeTrue(); // the rest of /api still traced

        // Positive control on the same route: the same include without the exclude.
        (await Traces(Config(include: ["/api"]), LiveResults)).ShouldBeTrue();
    }

    [Fact]
    public async Task HostAndQueryString_NeverCount()
    {
        var api = Config(include: ["/api"]);

        // "api" in the host and "api/" in the query string, path outside the list.
        (await Traces(api, "/home/index", host: "api.example.com", query: "?next=/api/orders&x=api/")).ShouldBeFalse();

        // Positive control on the same route, host and query.
        (await Traces(Config(include: ["/home"]), "/home/index", host: "api.example.com",
            query: "?next=/api/orders&x=api/")).ShouldBeTrue();

        // And the query string cannot defeat an include either: the path decides.
        (await Traces(api, "/api/orders", query: "?x=/home")).ShouldBeTrue();
    }

    [Fact]
    public async Task CaseIsIgnored_InIncludeAndExclude()
    {
        (await Traces(Config(include: ["/API/Contests"]), LiveResults)).ShouldBeTrue();
        (await Traces(Config(include: ["/api/contests"]), "/API/CONTESTS/5")).ShouldBeTrue();

        // Exclude in a different case still excludes.
        (await Traces(Config(include: ["/api"], exclude: ["/API/CONTESTS"]), LiveResults)).ShouldBeFalse();

        // Positive control on the same route.
        (await Traces(Config(include: ["/api"], exclude: ["/API/ORDERS"]), LiveResults)).ShouldBeTrue();
    }

    [Fact]
    public async Task SwitchFalse_StopsTracing_AndSkipsTheListReads_ReadPerRequest()
    {
        var enabled = false;
        var includeReads = 0;
        var excludeReads = 0;
        IReadOnlyCollection<string> include = ["/api"];
        IReadOnlyCollection<string> exclude = ["/api/never"];
        var config = new ApiConfiguration
        {
            InboundTraceEnabled = () => enabled,
            InboundIncludePaths = () => { includeReads++; return include; },
            InboundExcludePaths = () => { excludeReads++; return exclude; },
        };
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(config, sink);

        await middleware.InvokeAsync(BuildContext(LiveResults), WriteOkResponse);

        sink.Traces.ShouldBeEmpty();
        includeReads.ShouldBe(0);
        excludeReads.ShouldBe(0);

        // Positive control on the same route and the SAME instance: flipping the switch at runtime
        // takes effect on the next request, so it is not read once at construction.
        enabled = true;
        await middleware.InvokeAsync(BuildContext(LiveResults), WriteOkResponse);

        sink.Traces.ShouldHaveSingleItem();
        includeReads.ShouldBe(1);
        excludeReads.ShouldBe(1);

        // And back off again.
        enabled = false;
        await middleware.InvokeAsync(BuildContext(LiveResults), WriteOkResponse);
        sink.Traces.Count.ShouldBe(1);
        includeReads.ShouldBe(1);
    }

    [Fact]
    public async Task NullSwitch_IsOff()
    {
        var config = new ApiConfiguration { InboundIncludePaths = () => ["/api"] };
        (await Traces(config, "/api/orders")).ShouldBeFalse();

        // Positive control on the same route.
        config.InboundTraceEnabled = () => true;
        (await Traces(config, "/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangedList_AppliesOnTheNextRequest()
    {
        IReadOnlyCollection<string> include = ["/api/orders"];
        IReadOnlyCollection<string> exclude = [];
        var config = new ApiConfiguration
        {
            InboundTraceEnabled = () => true,
            InboundIncludePaths = () => include,
            InboundExcludePaths = () => exclude,
        };
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(config, sink);

        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        sink.Traces.Count.ShouldBe(1);

        include = ["/api/contests"];
        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        sink.Traces.Count.ShouldBe(1); // no longer included

        await middleware.InvokeAsync(BuildContext(LiveResults), WriteOkResponse);
        sink.Traces.Count.ShouldBe(2); // now included

        exclude = ["/api/contests/{id}/live-results"];
        await middleware.InvokeAsync(BuildContext(LiveResults), WriteOkResponse);
        sink.Traces.Count.ShouldBe(2); // newly excluded

        exclude = [];
        await middleware.InvokeAsync(BuildContext(LiveResults), WriteOkResponse);
        sink.Traces.Count.ShouldBe(3); // exclusion lifted
    }

    [Fact]
    public async Task UnchangedList_IsNotRecompiled()
    {
        var calls = 0;
        IReadOnlyCollection<string> sameInstance = ["/api/orders", "/api/contests", "/api/{id}/x"];
        var changed = false;
        var config = new ApiConfiguration
        {
            InboundTraceEnabled = () => true,
            InboundIncludePaths = () =>
            {
                calls++;
                if (changed)
                {
                    return ["/api/other"];
                }

                // Alternate the same instance with fresh instances holding the same contents in a
                // different order, case and placeholder spelling.
                return calls % 2 == 0 ? sameInstance : new List<string> { "/API/Contests/", "api/orders", "/API/{other}/x/" };
            },
        };
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(config, sink);

        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        var compiled = middleware.IncludePatterns.CurrentPatterns;
        middleware.IncludePatterns.CompileCount.ShouldBe(1);

        for (var i = 0; i < 6; i++)
        {
            await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        }

        calls.ShouldBe(7);
        sink.Traces.Count.ShouldBe(7);
        middleware.IncludePatterns.CompileCount.ShouldBe(1);
        middleware.IncludePatterns.CurrentPatterns.ShouldBeSameAs(compiled);

        // Positive control: a list whose contents differ IS recompiled, once.
        changed = true;
        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        middleware.IncludePatterns.CompileCount.ShouldBe(2);
        middleware.IncludePatterns.CurrentPatterns.ShouldNotBeSameAs(compiled);
        sink.Traces.Count.ShouldBe(7);
    }

    [Fact]
    public async Task ThrowingListFunction_DegradesToNoTrace_AndServesTheRequest()
    {
        var config = new ApiConfiguration
        {
            InboundTraceEnabled = () => true,
            InboundIncludePaths = () => throw new InvalidOperationException("settings store down"),
        };
        var factory = new CapturingLoggerFactory();
        var (traced, _, _, clientStream) = await InvokeWithStreams(config, "/api/orders", factory);

        traced.ShouldBeFalse();
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(ResponseBody);
        factory.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Error);

        // Positive control on the same route.
        (await Traces(Config(include: ["/api"]), "/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public async Task CancelledListFunction_DegradesToNoTrace_AndLogsBelowError()
    {
        var config = new ApiConfiguration
        {
            InboundTraceEnabled = () => true,
            InboundIncludePaths = () => throw new OperationCanceledException("request aborted"),
        };
        var factory = new CapturingLoggerFactory();
        var (traced, _, _, clientStream) = await InvokeWithStreams(config, "/api/orders", factory);

        traced.ShouldBeFalse();
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(ResponseBody);
        var entry = factory.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.Exception.ShouldBeOfType<OperationCanceledException>();

        // Positive control on the same route.
        (await Traces(Config(include: ["/api"]), "/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public async Task NullConfiguration_TracesNothing_AndServesTheRequest()
    {
        var sink = new CapturingSink();
        var context = BuildContext("/api/orders");
        var clientStream = new MemoryStream();
        context.Response.Body = clientStream;

        await new ApiTraceMiddleware(null, sink).InvokeAsync(context, WriteOkResponse);
        await new ApiTraceMiddleware().InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);

        sink.Traces.ShouldBeEmpty();
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(ResponseBody);

        // Positive control on the same route.
        (await Traces(Config(include: ["/api"]), "/api/orders")).ShouldBeTrue();
    }

    [Fact]
    public async Task MsDiOnlyConsumer_WithoutApiConfiguration_ResolvesAndTracesNothing()
    {
        // A consumer on plain MS-DI that registers the middleware but no ApiConfiguration must get a
        // working middleware with tracing off, not an activation failure (a 500 on every request).
        var sink = new CapturingSink();
        var services = new ServiceCollection();
        services.AddSingleton<IApiTraceSink>(sink);
        services.AddSingleton<ApiTraceMiddleware>();
        using (var provider = services.BuildServiceProvider())
        {
            var middleware = provider.GetRequiredService<ApiTraceMiddleware>();
            await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        }

        sink.Traces.ShouldBeEmpty();

        // Positive control on the same route: with a registered configuration, MS-DI injects that
        // singleton and the request is traced.
        var config = Config(include: ["/api"]);
        services.AddSingleton(config);
        using (var provider = services.BuildServiceProvider())
        {
            var middleware = provider.GetRequiredService<ApiTraceMiddleware>();
            typeof(ApiTraceMiddleware).GetField("_apiConfiguration", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(middleware).ShouldBeSameAs(config);
            await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        }

        sink.Traces.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SuspiciousPatterns_WarnOncePerEntryOnRebuild_NeverPerRequest()
    {
        var calls = 0;
        IReadOnlyCollection<string> include = ["/api", "/api/*", "https://self.example.com/api/x", "/api/a%20b",
            "/api/orders?page=1", "/api/*"];
        var config = new ApiConfiguration
        {
            InboundTraceEnabled = () => true,
            // A fresh instance with the same contents on every call: the same-set path, not a rebuild.
            InboundIncludePaths = () =>
            {
                calls++;
                return new List<string>(include);
            },
        };
        var factory = new CapturingLoggerFactory();
        var sink = new CapturingSink();
        var middleware = new ApiTraceMiddleware(config, sink, factory);

        for (var i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        }

        calls.ShouldBe(5);
        sink.Traces.Count.ShouldBe(5);
        var warnings = factory.Entries.FindAll(e => e.Level == LogLevel.Warning);
        warnings.Count.ShouldBe(4); // one per distinct suspicious entry, once
        warnings.ShouldAllBe(w => w.LoggerName == "ApiTraceMiddleware");

        // A changed list warns again, only for what it holds.
        include = ["/api", "/other/*"];
        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        await middleware.InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        factory.Entries.FindAll(e => e.Level == LogLevel.Warning).Count.ShouldBe(5);

        // Positive control: a clean list warns nothing.
        var clean = new CapturingLoggerFactory();
        await new ApiTraceMiddleware(Config(include: ["/api", "/api/contests/{id}/live-results"]), sink, clean)
            .InvokeAsync(BuildContext("/api/orders"), WriteOkResponse);
        clean.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("/api", "/api/", true)]
    [InlineData("/api/", "/api", true)]
    [InlineData("api", "/api/orders", true)]
    [InlineData("/api", "//api//orders", true)]
    [InlineData("/api/orders", "/api", false)]
    [InlineData("/{any}", "/home", true)]
    [InlineData("/{any}", "/", false)]
    [InlineData("/api/{id}", "/api", false)]
    public void PatternMatching_BySegment(string pattern, string path, bool expected)
    {
        var key = InboundTracePathPattern.Normalise(pattern);
        key.ShouldNotBeNull();
        InboundTracePathPattern.FromKey(key).Matches(path).ShouldBe(expected);
    }

    #region Test Infrastructure

    private static ApiConfiguration Config(IReadOnlyCollection<string> include,
        IReadOnlyCollection<string>? exclude = null) => new()
    {
        InboundTraceEnabled = () => true,
        InboundIncludePaths = () => include,
        InboundExcludePaths = exclude is null ? null : () => exclude,
    };

    private static async Task<bool> Traces(ApiConfiguration config, string path,
        string host = "self.example.com", string? query = null)
    {
        var sink = new CapturingSink();
        await new ApiTraceMiddleware(config, sink).InvokeAsync(BuildContext(path, host, query), WriteOkResponse);
        return sink.Traces.Count == 1;
    }

    private static async Task<(bool Traced, HttpContext Context, ReadCountingStream RequestBody, MemoryStream ClientStream)>
        InvokeWithStreams(ApiConfiguration config, string path, CapturingLoggerFactory? loggerFactory = null)
    {
        var sink = new CapturingSink();
        var context = BuildContext(path);
        var requestBody = new ReadCountingStream(Encoding.UTF8.GetBytes("{\"id\":7}"));
        context.Request.Body = requestBody;
        context.Request.ContentLength = null;
        var clientStream = new MemoryStream();
        context.Response.Body = clientStream;
        Stream? seenByHandler = null;

        await new ApiTraceMiddleware(config, sink, loggerFactory ?? new CapturingLoggerFactory()).InvokeAsync(context, async c =>
        {
            seenByHandler = c.Response.Body;
            await WriteOkResponse(c);
        });

        if (sink.Traces.Count == 0)
        {
            seenByHandler.ShouldBeSameAs(clientStream);
        }

        return (sink.Traces.Count == 1, context, requestBody, clientStream);
    }

    /// <summary>Non-seekable request body that counts reads, like the server's own.</summary>
    private sealed class ReadCountingStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public int Reads { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            return _inner.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Reads++;
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Reads++;
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static async Task WriteOkResponse(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync(ResponseBody);
    }

    private static DefaultHttpContext BuildContext(string path, string host = "self.example.com",
        string? query = null)
    {
        var context = new DefaultHttpContext();
        var body = Encoding.UTF8.GetBytes("{\"id\":7}");

        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        if (query is not null)
        {
            context.Request.QueryString = new QueryString(query);
        }

        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = body.Length;
        context.Request.Body = new MemoryStream(body);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class CapturingSink : IApiTraceSink
    {
        public List<ApiTraceData> Traces { get; } = [];

        public void Save(ApiTraceData trace) => Traces.Add(trace);
    }

    #endregion
}
