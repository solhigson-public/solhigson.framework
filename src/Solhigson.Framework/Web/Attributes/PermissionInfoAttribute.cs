using System;

namespace Solhigson.Framework.Web.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PermissionInfoAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; set; } = description;
    public bool IsMenuRoot { get; set; }
    public bool IsMenu { get; set; }
    public int MenuIndex { get; set; }
    public string? Icon { get; set; }
    public string? ParentName { get; set; }
    public string[]? AllowedRoles { get; set; }
    internal string? Url { get; set; }
    public string? OnClickFunction { get; set; }
}