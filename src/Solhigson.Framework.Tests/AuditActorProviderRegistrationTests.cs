using Autofac;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Solhigson.Framework.Extensions;
using Xunit;

namespace Solhigson.Framework.Tests;

public class AuditActorProviderRegistrationTests
{
    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    [Fact]
    public void NoConsumerRegistration_ResolvesUnattributedDefault()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var provider = scope.Resolve<IAuditActorProvider>();

        provider.ShouldBeOfType<UnattributedAuditActorProvider>();
        var actor = provider.GetCurrentActor();
        actor.SourceType.ShouldBe(AuditActor.Unattributed);
        actor.UserDisplayName.ShouldBe(AuditActor.Unattributed);
        actor.ActorUserId.ShouldBeNull();
    }

    [Fact]
    public void ConsumerRegistration_OverridesDefault_PreserveExistingDefaults()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TestActorProvider>().As<IAuditActorProvider>().InstancePerLifetimeScope();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var provider = scope.Resolve<IAuditActorProvider>();

        provider.ShouldBeOfType<TestActorProvider>();
        provider.GetCurrentActor().ActorUserId.ShouldBe("consumer-actor");
    }

    [Fact]
    public void CaptureRegistry_ResolvesAsSingleton()
    {
        var builder = new ContainerBuilder();
        builder.RegisterSolhigsonDependencies(EmptyConfiguration());
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var first = scope.Resolve<AuditCaptureRegistry>();
        var second = scope.Resolve<AuditCaptureRegistry>();

        first.ShouldNotBeNull();
        first.ShouldBeSameAs(second);
    }

    private sealed class TestActorProvider : IAuditActorProvider
    {
        public AuditActor GetCurrentActor() => new() { ActorUserId = "consumer-actor" };
    }
}
