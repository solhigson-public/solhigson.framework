using System;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Xunit;

namespace Solhigson.Framework.Tests;

public class AuditCaptureRegistryTests
{
    [Fact]
    public void Include_ThenIsIncluded_ReturnsTrue()
    {
        var registry = new AuditCaptureRegistry();

        registry.Include<Included>();

        registry.IsIncluded(typeof(Included)).ShouldBeTrue();
    }

    [Fact]
    public void UnregisteredType_IsNotIncluded()
    {
        var registry = new AuditCaptureRegistry();

        registry.IsIncluded(typeof(Unregistered)).ShouldBeFalse();
    }

    [Fact]
    public void Ignore_WinsOverInclude_WhenIncludedFirst()
    {
        var registry = new AuditCaptureRegistry();

        registry.Include<Contested>().Ignore<Contested>();

        registry.IsIncluded(typeof(Contested)).ShouldBeFalse();
    }

    [Fact]
    public void Ignore_WinsOverInclude_WhenIgnoredFirst()
    {
        var registry = new AuditCaptureRegistry();

        registry.Ignore<Contested>().Include<Contested>();

        registry.IsIncluded(typeof(Contested)).ShouldBeFalse();
    }

    [Fact]
    public void Include_IsFluent_ReturnsSameRegistry()
    {
        var registry = new AuditCaptureRegistry();

        registry.Include<Included>().ShouldBeSameAs(registry);
    }

    [Fact]
    public void IsIncluded_NullType_Throws()
    {
        var registry = new AuditCaptureRegistry();

        Should.Throw<ArgumentNullException>(() => registry.IsIncluded(null!));
    }

    private sealed class Included
    {
    }

    private sealed class Contested
    {
    }

    private sealed class Unregistered
    {
    }
}
