namespace Solhigson.Framework.Dto;

public record PermissionInfoDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int MenuIndex { get; init; }
    public string[]? AllowedRoles { get; init; }
    public string? OnClickFunction { get; init; }
    public bool IsMenuRoot { get; init; }
    public bool IsMenu { get; init; }
    public string? Url { get; set; }
    public string? Icon { get; init; }
    public string? ParentName { get; init; }

}