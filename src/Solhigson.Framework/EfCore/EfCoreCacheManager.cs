using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Utilities;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore;

internal static class EfCoreCacheManager
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(EfCoreCacheManager).FullName);
    private static IDatabase? _database;
    private static string? _prefix;

    internal static void Initialize(IConnectionMultiplexer? redis, string? prefix = "solhigson.efcore.caching.")
    {
        try
        {
            if (redis is null)
            {
                Logger.LogWarning("Unable to initialize EfCore Cache Manager because Redis is not configured");
                return;
            }
            _database = redis.GetDatabase();
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

    private static string GetTagKey(Type type)
    {
        return _prefix + type.Name;
    }
    
    internal static async Task<bool> InvalidateAsync(Type[] types)
    {
        if (_database is null || !IsICachedEntity(types, out var validTypes))
        {
            return false;
        }
        List<string> cacheKeys = [];
        var tran = _database.CreateTransaction();
        
        foreach (var type in validTypes)
        {
            var tagCacheKey = GetTagKey(type);
            var values = await _database.SetMembersAsync(tagCacheKey);
            if (values.Length != 0)
            {
                cacheKeys.AddRange(values.Select(value => value.ToString()));
            }
            await tran.KeyDeleteAsync(tagCacheKey);
        }
        foreach (var cacheKey in cacheKeys)
        {
            await tran.KeyDeleteAsync(cacheKey);
        }
        
        return await tran.ExecuteAsync();
    }

    private static bool IsICachedEntity(Type[] types, out Type[] validTypes)
    {
        validTypes = types?.Where(type => typeof(ICachedEntity).IsAssignableFrom(type)).ToArray() ?? [];
        return validTypes.HasData();
    }
    
    internal static async Task<bool> SetDataAsync<T>(string key, T? data, Type[] types) where T : class
    {
        try
        {
            if (_database is null || string.IsNullOrWhiteSpace(key) || data is null
                || !IsICachedEntity(types, out var validTypes))
            {
                return false;
            }
            
            var tran = _database.CreateTransaction();
            var cacheKey = GetKey(key);
            foreach (var type in validTypes)
            {
                await tran.SetAddAsync(GetTagKey(type), cacheKey);
            }

            await tran.StringSetAsync(GetKey(key), data.SerializeToJson());
            return await tran.ExecuteAsync();

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
            if (_database is null || string.IsNullOrWhiteSpace(key))
            {
                return response.Fail();
            }

            var resp = await _database.StringGetAsync(GetKey(key));
            string? json = resp;
            if (string.IsNullOrWhiteSpace(json))
            {
                return response.Fail();
            }
            
            try
            {
                return response.Success(json.DeserializeFromJson<T>());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "While trying to deserialize {entry} into type {type}", json, typeof(T));
            }
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