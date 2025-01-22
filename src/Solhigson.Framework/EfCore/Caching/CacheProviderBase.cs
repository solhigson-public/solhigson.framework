using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore.Caching;

public abstract class CacheProviderBase(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440)
    : ICacheProvider
{
    protected readonly IDatabase Database = redis.GetDatabase();
    protected readonly int ExpirationInMinutes = expirationInMinutes;

    protected string GetTagKey(Type type)
    {
        return prefix + EfCoreCacheManager.GetTypeName(type);
    }


    public abstract Task<bool> InvalidateCacheAsync(Type[] types);

    public abstract Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types) where T : class;

    public abstract Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey) where T : class;
}