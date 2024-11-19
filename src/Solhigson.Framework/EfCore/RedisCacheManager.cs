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

internal static class RedisCacheManager
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(RedisCacheManager).FullName);
    private static IDatabase? _database;
    private static string? _prefix;

    internal static void Initialize(IConnectionMultiplexer redis, string prefix = "solhigson.efcore.caching.")
    {
        try
        {
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

    
    internal static async Task<bool> SetDataAsync<T>(string key, T? data, IList<Type>? types, TimeSpan? timeSpan = null) where T : class
    {
        if (types?.Any(type => typeof(ICachedEntity).IsAssignableFrom(type)) == false)
        {
            return false;
        }
        try
        {
            if (_database is not null && !string.IsNullOrWhiteSpace(key) && data is not null)
            {
                return await _database.StringSetAsync(GetKey(key), data.SerializeToJson(), timeSpan);
            }
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

}