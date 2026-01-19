using System;
using Solhigson.Framework.Identity;

namespace Solhigson.Framework.Web.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PermissionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public bool IsPrimaryUrl { get; set; }

    [Obsolete("Specify the description in the PermissionInfoAttribute instead.")]
    public string Description { get; set; } = null!;
    [Obsolete("Specify the description in the PermissionInfoAttribute instead.")]
    public bool IsMenuRoot { get; set; }
    [Obsolete("Specify the description in the PermissionInfoAttribute instead.")]
    public bool IsMenu { get; set; }
    [Obsolete("Specify the description in the PermissionInfoAttribute instead.")]
    public int MenuIndex { get; set; }
    [Obsolete("Specify the description in the PermissionInfoAttribute instead.")]
    public string? Icon { get; set; }
}