using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Solhigson.Framework.Infrastructure;

internal static class ServiceProviderWrapper
{
    internal static IServiceProvider? ServiceProvider { get; set; }

    internal static IHttpContextAccessor? GetHttpContextAccessor()
    {
        try
        {
            return ServiceProvider?.GetService<IHttpContextAccessor>();
        }
        catch (Exception)
        {
            //
        }

        return null;
    }

    private static ScopedProperties? GetScopedProperties()
    {
        var serviceProvider = GetHttpContextAccessor()?.HttpContext?.RequestServices ?? ServiceProvider;
        CurrentLogScopedPropertiesAccessor? accessor = null;
        try
        {
            accessor = serviceProvider?.GetService<CurrentLogScopedPropertiesAccessor>();
        }
        catch (Exception)
        {
            //
        }
        if (accessor is null)
        {
            return null;
        }

        return accessor.ScopedProperties ??= new ScopedProperties();
    }

    private const string ChainId = "::solhigson::framework::LogItems::chainid::";
    internal static void SetCurrentLogChainId(string chainId)
    {
        var httpContext = GetHttpContextAccessor()?.HttpContext;
        if (httpContext is not null)
        {
            SetItem(httpContext, ChainId, chainId);
            return;
        }
        var scopedProperties = GetScopedProperties();
        if (scopedProperties is not null)
        {
            scopedProperties.LogChainId = chainId;
        }
    }
    private const string Email = "::solhigson::framework::LogItems::email::";
    internal static void SetCurrentLogUserEmail(string email)
    {
        var httpContext = GetHttpContextAccessor()?.HttpContext;
        if (httpContext is not null)
        {
            SetItem(httpContext, Email, email);
            return;
        }
        var scopedProperties = GetScopedProperties();
        if (scopedProperties is not null)
        {
            scopedProperties.UserEmail = email;
        }
    }
    
    internal static string? GetCurrentLogChainId()
    {
        if (GetHttpContextAccessor()?.HttpContext?.Items.TryGetValue(ChainId, out var chainId) == true)
        {
            return chainId as string;
        }
        return GetScopedProperties()?.LogChainId;
    }
    internal static string? GetCurrentLogUserEmail()
    {
        if (GetHttpContextAccessor()?.HttpContext?.Items.TryGetValue(Email, out var email) == true)
        {
            return email as string;
        }
        return GetScopedProperties()?.UserEmail;
    }

    private static void SetItem(HttpContext? context, string key, string value)
    {
        if (context is null)
        {
            return;
        }

        if (context.Items.ContainsKey(key))
        {
            context.Items[key] = value;
            return;
        }
        context.Items.Add(key, value);
    }
    

}