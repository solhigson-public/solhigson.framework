using System;

namespace Solhigson.Framework.Infrastructure.Dependency;

[AttributeUsage(AttributeTargets.Class)]
public class DependencyInstanceTypeAttribute(params Type[] types) : Attribute
{
    public Type[] Types => types;
}