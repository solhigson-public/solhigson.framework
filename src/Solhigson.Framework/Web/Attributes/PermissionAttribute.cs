using System;

namespace Solhigson.Framework.Web.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PermissionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public bool IsPrimaryUrl { get; set; }
}