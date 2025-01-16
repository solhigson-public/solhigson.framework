using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;
using Solhigson.Utilities;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore;

public class RedisCacheProvider : ICacheProvider
{
    private readonly IDatabase _database;
    private readonly string _prefix;
    private readonly int _expirationInMinutes;

    internal RedisCacheProvider(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440)
    {
        _database = redis.GetDatabase();
        _prefix = prefix;
        _expirationInMinutes = expirationInMinutes;
    }
    
    private string GetTagKey(Type type)
    {
        return _prefix + type.Name;
    }
    
    public async Task<bool> InvalidateCacheAsync(Type[] types)
    {
        List<string> cacheKeys = [];
        var tran = _database.CreateTransaction();
        
        foreach (var type in types)
        {
            var tagCacheKey = GetTagKey(type);
            var values = await _database.SetMembersAsync(tagCacheKey);
            if (values.Length != 0)
            {
                cacheKeys.AddRange(values.Select(value => value.ToString()));
            }
            _ = tran.KeyDeleteAsync(tagCacheKey);
        }
        foreach (var cacheKey in cacheKeys)
        {
            _ = tran.KeyDeleteAsync(cacheKey);
        }
        
        return await tran.ExecuteAsync();
    }

    public async Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types) where T : class
    {
        var tran = _database.CreateTransaction();
        foreach (var type in types)
        {
            _ = tran.SetAddAsync(GetTagKey(type), cacheKey);
        }

        _ = tran.StringSetAsync(cacheKey, data.SerializeToJson(), TimeSpan.FromMinutes(_expirationInMinutes));
        return await tran.ExecuteAsync();
    }

    public async Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string? cacheKey) where T : class
    {
        var response = new ResponseInfo<T?>();
        var resp = await _database.StringGetAsync(cacheKey);
        string? json = resp;
        return string.IsNullOrWhiteSpace(json) 
            ? response.Fail() 
            : response.Success(json.DeserializeFromJson<T>());
    }
}