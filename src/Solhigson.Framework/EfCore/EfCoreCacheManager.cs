using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore;

internal static class EfCoreCacheManager
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(EfCoreCacheManager).FullName);
    private static string? _prefix;
    private static ICacheProvider? _cacheProvider;

    internal static void Initialize(CacheType cacheType, IConnectionMultiplexer? redis, string? prefix = null,
        int expirationInMinutes = 1440, int changeTrackerTimerIntervalInSeconds = 5)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "solhigson.efcore.caching.";
            }
            if (redis is null)
            {
                Logger.LogWarning("Unable to initialize EfCore Cache Manager because Redis is not configured");
                return;
            }
            _cacheProvider = cacheType == CacheType.Redis 
                ? new RedisCacheProvider(redis, prefix, expirationInMinutes)
                : new MemoryCacheProvider(redis, prefix, expirationInMinutes, changeTrackerTimerIntervalInSeconds);
            
            _prefix = prefix;
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }
    
    private static string GetKey(string key)
    {
        return _prefix + key;
    }

    internal static async Task<bool> InvalidateAsync(Type[] types)
    {
        try
        {
            if (_cacheProvider is null || !IsICachedEntity(types, out var validTypes))
            {
                return false;
            }
            return await _cacheProvider.InvalidateCacheAsync(validTypes);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        return false;
    }

    private static bool IsICachedEntity(Type[]? types, out Type[] validTypes)
    {
        validTypes = types?.Where(type => typeof(ICachedEntity).IsAssignableFrom(type)).ToArray() ?? [];
        return validTypes.HasData();
    }
    
    internal static async Task<bool> SetDataAsync<T>(string key, T? data, Type[] types) where T : class
    {
        try
        {
            if (_cacheProvider is null || string.IsNullOrWhiteSpace(key) || data is null
                || !IsICachedEntity(types, out var validTypes))
            {
                return false;
            }
            return await _cacheProvider.AddToCacheAsync(GetKey(key), data, validTypes);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        return false;
    }
    
    internal static async Task<ResponseInfo<T?>> GetDataAsync<T>(string? key) where T : class
    {
        var response = new ResponseInfo<T?>();
        try
        {
            if (_cacheProvider is null || string.IsNullOrWhiteSpace(key))
            {
                return response.Fail();
            }
            return await _cacheProvider.GetFromCacheAsync<T>(GetKey(key));
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        return response.Fail();
    }
    
    internal static string Flatten(IEnumerable<Type> iCacheEntityTypes)
    {
        var result = "";
        foreach (var type in iCacheEntityTypes)
        {
            result = string.IsNullOrEmpty(result) 
                ? type.Name 
                : $"{result}-{type.Name}";
        }
        return result;
    }


}