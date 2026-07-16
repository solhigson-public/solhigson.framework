using Autofac;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Solhigson.Framework.Extensions;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Availability contract: the F2-prime interceptor, the F3-prime service, and the append-only interceptor all
/// register unconditionally in <c>SolhigsonAutofacModule.Load</c> (so the consumer can resolve and
/// <c>AddInterceptors</c> them on its own AppDbContext), and their DI graph closes even though the never-block
/// <see cref="IAuditSink"/> seam is DEFINED framework-side but NOT concretely registered by the framework (the
/// capture interceptor's <see cref="IAuditSink"/> dependency is OPTIONAL and resolves to null in a
/// framework-only container). They are singletons because a pooled DbContext captures the interceptor across
/// scopes. The M1 outermost-commit trigger folds into the existing
/// <see cref="AuditCaptureSaveChangesInterceptor"/> (it also implements <c>IDbTransactionInterceptor</c>) — no
/// separate companion type is registered, so no extra registration assertion is required.
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

    [Fact]
    public void AuditSinkSeam_IsDefinedButNotConcretelyRegisteredByTheFramework()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();

        // The framework OWNS the IAuditSink abstraction but MUST NOT register a concrete implementation
        // (the consumer provides + wires it). The capture interceptor still resolves — its IAuditSink
        // dependency is OPTIONAL and null in a framework-only container.
        scope.IsRegistered<IAuditSink>().ShouldBeFalse();
        scope.ResolveOptional<IAuditSink>().ShouldBeNull();
        scope.Resolve<AuditCaptureSaveChangesInterceptor>().ShouldNotBeNull();
    }
}
