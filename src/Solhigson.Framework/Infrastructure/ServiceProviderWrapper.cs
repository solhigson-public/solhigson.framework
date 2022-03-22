using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Solhigson.Framework.Infrastructure;

public static class ServiceProviderWrapper
{
    public static IServiceProvider ServiceProvider { get; set; }

    public static HttpContextAccessor HttpContextAccessor => ServiceProvider?.GetService<HttpContextAccessor>();

    private static ScopedProperties GetScopedProperties()
    {
        var accessor = ServiceProvider?.GetService<CurrentLogScopedPropertiesAccessor>();
        if (accessor is null)
        {
            return null;
        }

        return accessor.ScopedProperties ??= new ScopedProperties();
    }
    internal static void SetCurrentLogChainId(string chainId)
    {
        var scopedProperties = GetScopedProperties();
        if (scopedProperties is not null)
        {
            scopedProperties.LogChainId = chainId;
        }
    }
    internal static void SetCurrentLogUserEmail(string email)
    {
        var scopedProperties = GetScopedProperties();
        if (scopedProperties is not null)
        {
            scopedProperties.UserEmail = email;
        }
    }

    internal static string CurrentLogChainId => ServiceProvider?.GetService<CurrentLogScopedPropertiesAccessor>()?.ScopedProperties?.LogChainId;
    internal static string UserEmail => ServiceProvider?.GetService<CurrentLogScopedPropertiesAccessor>()?.ScopedProperties?.UserEmail;
}