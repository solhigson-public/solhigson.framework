using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore.Caching;

internal static class EfCoreCacheManager
{
    private static LogWrapper? _logger;
    private static string? _prefix;
    private static ICacheProvider? _cacheProvider;

    internal static void Initialize(CacheType cacheType, IConnectionMultiplexer? redis, string? prefix = null,
        int expirationInMinutes = 1440, int changeTrackerTimerIntervalInSeconds = 5)
    {
        try
        {
            _logger = LogManager.GetLogger(typeof(EfCoreCacheManager).FullName);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "solhigson.efcore.caching.";
            }
            if (redis is null)
            {
                _logger.LogWarning("Unable to initialize EfCore Cache Manager because Redis is not configured");
                return;
            }
            _cacheProvider = cacheType == CacheType.Redis 
                ? new RedisCacheProvider(redis, prefix, expirationInMinutes)
                : new MemoryCacheProvider(redis, prefix, expirationInMinutes, changeTrackerTimerIntervalInSeconds);
            
            _prefix = prefix;
        }
        catch (Exception e)
        {
            _logger?.LogError(e);
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
            _logger?.LogError(e);
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
            _logger?.LogError(e, "Unable to add data of type {type} to cache, " +
                                 "type might not serializable to json consider using CacheType.Memory", typeof(T).FullName);
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
            _logger?.LogError(e);
        }

        return response.Fail();
    }

    internal static string GetTypeName(Type type)
    {
        return type.FullName!;
    }

}