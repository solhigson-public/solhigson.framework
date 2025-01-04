using System;

namespace Solhigson.Framework.Infrastructure.Dependency;

[AttributeUsage(AttributeTargets.Class)]
public class DependencyInjectAttribute : Attribute
{
    public DependencyLifetime DependencyLifetime { get; set; } = DependencyLifetime.Scoped;
    public Type[]? RegisteredTypes { get; set; }
}