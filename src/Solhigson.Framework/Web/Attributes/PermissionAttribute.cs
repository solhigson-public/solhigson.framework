using System;

namespace Solhigson.Framework.Web.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; set; } = null!;
    public bool IsMenuRoot { get; set; }
    public bool IsMenu { get; set; }
    public int MenuIndex { get; set; }
    public string? Icon { get; set; }
}