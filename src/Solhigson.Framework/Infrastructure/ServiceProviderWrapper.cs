using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Solhigson.Framework.Infrastructure;

internal static class ServiceProviderWrapper
{
    internal static IServiceProvider ServiceProvider { get; set; }

    internal static IHttpContextAccessor HttpContextAccessor => ServiceProvider?.GetService<IHttpContextAccessor>();

    private static ScopedProperties GetScopedProperties()
    {
        var serviceProvider = HttpContextAccessor?.HttpContext?.RequestServices ?? ServiceProvider;
        var accessor = serviceProvider?.GetService<CurrentLogScopedPropertiesAccessor>();
        if (accessor is null)
        {
            return null;
        }

        return accessor.ScopedProperties ??= new ScopedProperties();
    }

    private const string ChainId = "::solhigson::framework::LogItems::chainid::";
    internal static void SetCurrentLogChainId(string chainId)
    {
        var httpContext = HttpContextAccessor?.HttpContext;
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
        var httpContext = HttpContextAccessor?.HttpContext;
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
    
    internal static string GetCurrentLogChainId()
    {
        if (HttpContextAccessor?.HttpContext?.Items.TryGetValue(ChainId, out var chainId) == true)
        {
            return chainId as string;
        }
        return GetScopedProperties()?.LogChainId;
    }
    internal static string GetCurrentLogUserEmail()
    {
        if (HttpContextAccessor?.HttpContext?.Items.TryGetValue(Email, out var email) == true)
        {
            return email as string;
        }
        return GetScopedProperties()?.UserEmail;
    }

    private static void SetItem(HttpContext context, string key, string value)
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