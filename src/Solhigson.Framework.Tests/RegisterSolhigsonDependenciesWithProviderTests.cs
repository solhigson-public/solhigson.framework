using System;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Persistence;
using Xunit;

namespace Solhigson.Framework.Tests;

public class RegisterSolhigsonDependenciesWithProviderTests
{
    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    [Fact]
    public void RegisterSolhigsonDependenciesWithProvider_RelationalProviderConfigured_ResolvesDbContext()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterSolhigsonDependenciesWithProvider(
            EmptyConfiguration(),
            opt => opt.UseSqlite("Data Source=:memory:"));

        var container = containerBuilder.Build();

        using var scope = container.BeginLifetimeScope();
        var context = scope.Resolve<SolhigsonDbContext>();

        context.ShouldNotBeNull();
    }

    [Fact]
    public void RegisterSolhigsonDependenciesWithProvider_NoProviderConfigured_ThrowsOnBuild()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterSolhigsonDependenciesWithProvider(
            EmptyConfiguration(),
            opt => { });

        var exception = Should.Throw<InvalidOperationException>(() => containerBuilder.Build());

        exception.Message.ShouldContain("did not configure a database provider");
    }
}
