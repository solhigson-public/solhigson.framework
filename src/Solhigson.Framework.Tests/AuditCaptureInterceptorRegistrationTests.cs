using Autofac;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Solhigson.Framework.Extensions;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Deliverable-4 availability contract: both F2 interceptors register in <c>SolhigsonAutofacModule.Load</c>
/// (so the consumer can resolve and <c>AddInterceptors</c> them on its own AppDbContext), and their DI graph
/// closes — the capture interceptor's three constructor seams (actor provider, registry, options) all resolve.
/// They are singletons because a pooled DbContext captures the interceptor across scopes.
/// </summary>
public class AuditCaptureInterceptorRegistrationTests
{
    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    [Fact]
    public void CaptureInterceptor_ResolvesAsSingleton()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var first = scope.Resolve<AuditCaptureSaveChangesInterceptor>();
        var second = scope.Resolve<AuditCaptureSaveChangesInterceptor>();

        first.ShouldNotBeNull();
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void AppendOnlyInterceptor_ResolvesAsSingleton()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var first = scope.Resolve<AuditTrailAppendOnlyInterceptor>();
        var second = scope.Resolve<AuditTrailAppendOnlyInterceptor>();

        first.ShouldNotBeNull();
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void CaptureOptions_ResolvesAsSingleton()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var first = scope.Resolve<AuditCaptureOptions>();
        var second = scope.Resolve<AuditCaptureOptions>();

        first.ShouldNotBeNull();
        first.ShouldBeSameAs(second);
    }
}
