using System;

namespace Solhigson.Framework.Web.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PermissionInfoAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public int MenuIndex { get; set; }
    public string[] AllowedRoles { get; set; } = [];
    public string? OnClickFunction { get; set; }
    
    
    internal bool IsMenuRoot { get; init; }
    internal bool IsMenu { get; init; }
    internal string? Url { get; set; }
    internal string? Icon { get; init; }
    internal string? ParentName { get; init; }
}

public abstract class PermissionInfoMenuAttribute(string name, string description) : PermissionInfoAttribute(name, description);


public class PermissionInfoMenuRootAttribute(string name, string description, string? icon = null)
    : PermissionInfoMenuAttribute(name, description);

public class PermissionInfoChildMenuAttribute(string name, string description, string parent)
    : PermissionInfoMenuAttribute(name, description);

public class PermissionInfoChildNonMenuAttribute(string name, string description, string parent) : PermissionInfoAttribute(name, description);