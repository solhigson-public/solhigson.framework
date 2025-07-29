using System.Collections.Generic;
using System.Threading;

namespace Solhigson.Framework.Infrastructure;

internal class CurrentLogScopedPropertiesAccessor
{
    private static readonly AsyncLocal<ScopedPropertiesHolder> CurrentScopedProperties = new();

    internal ScopedProperties? ScopedProperties
    {
        get => CurrentScopedProperties.Value?.ScopedProperties;
        set
        {
            var props = CurrentScopedProperties.Value;
            if (props is not null)
            {
                props.ScopedProperties = null;
            }

            if (value is not null)
            {
                CurrentScopedProperties.Value = new ScopedPropertiesHolder { ScopedProperties = value };
            }
        }
    }

    /*
    public void SetCurrentLogChainId(string chainId)
    {
        if (ScopedProperties is not null)
        {
            ScopedProperties.LogChainId = chainId;
        }
    }

    public void SetCurrentLogUserEmail(string email)
    {
        if (ScopedProperties is not null)
        {
            ScopedProperties.UserEmail = email;
        }
    }
    */

    private class ScopedPropertiesHolder
    {
        public ScopedProperties? ScopedProperties;
    }


}
internal class ScopedProperties
{
    private const string ChainId = "chainId";
    private const string Email = "email";

    public void AddProperty(string key, string value)
    {
        Properties ??= new Dictionary<string, object>();
        Properties.TryAdd(key, value);
    }

    internal void AddChainId(string chainId)
    {
        AddProperty(ChainId, chainId);
    }
    
    internal void AddEmail(string email)
    {
        AddProperty(Email, email);
    }

    internal string? GetChainId()
    {
        return GetProperty(ChainId)?.ToString();
    }

    internal string? GetEmail()
    {
        return GetProperty(Email)?.ToString();
    }
    
    private object? GetProperty(string key)
    {
        object? value = null;
        Properties?.TryGetValue(key, out value);
        return value;
    }

    internal Dictionary<string, object>? Properties { get; private set; }
}