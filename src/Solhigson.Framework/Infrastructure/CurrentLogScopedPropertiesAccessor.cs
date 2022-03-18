namespace Solhigson.Framework.Infrastructure;

internal class CurrentLogScopedPropertiesAccessor
{
    internal ScopedProperties ScopedProperties { get; set; }
}

internal class ScopedProperties
{
    public string LogChainId { get; set; }
    public string UserEmail { get; set; }
}