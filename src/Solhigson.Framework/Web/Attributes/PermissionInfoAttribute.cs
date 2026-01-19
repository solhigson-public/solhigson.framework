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

public class PermissionInfoMenuRootAttribute : PermissionInfoMenuAttribute
{
    public PermissionInfoMenuRootAttribute(string name, string description, string? icon = null) : base(name, description)
    {
        IsMenuRoot = true;
        Icon = icon;
    }
}

public class PermissionInfoMenuAttribute : PermissionInfoAttribute
{
    public PermissionInfoMenuAttribute(string name, string description) : base(name, description)
    {
        IsMenu = true;
    }
}

public class PermissionInfoChildMenuAttribute : PermissionInfoMenuAttribute
{
    public PermissionInfoChildMenuAttribute(string name, string description, string parent) : base(name, description)
    {
        ParentName = parent;
    }
}

public class PermissionInfoChildNonMenuAttribute : PermissionInfoAttribute
{
    public PermissionInfoChildNonMenuAttribute(string name, string description, string parent) : base(name, description)
    {
        ParentName = parent;
        IsMenuRoot = false;
        IsMenu = false;
    }
}