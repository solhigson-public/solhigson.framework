using System;

namespace Solhigson.Framework.Infrastructure.Dependency;

[AttributeUsage(AttributeTargets.Class)]
public class DependencyInjectAttribute : Attribute
{
    public DependencyInjectAttribute()
    {
        DependencyType = DependencyType.Scoped;
    }
    public DependencyType DependencyType { get; set; }
}