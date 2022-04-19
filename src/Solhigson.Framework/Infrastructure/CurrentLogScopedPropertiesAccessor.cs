using System.Threading;

namespace Solhigson.Framework.Infrastructure;

internal class CurrentLogScopedPropertiesAccessor
{
    private static readonly AsyncLocal<ScopedPropertiesHolder> CurrentScopedProperties = new();

    internal ScopedProperties ScopedProperties
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
        public ScopedProperties ScopedProperties;
    }


}
internal class ScopedProperties
{
    public string LogChainId { get; set; }
    public string UserEmail { get; set; }
}