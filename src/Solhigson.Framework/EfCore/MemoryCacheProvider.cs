using System;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore;

public class MemoryCacheProvider : ICacheProvider
{
    public MemoryCacheProvider(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440,
        int changeTrackerTimerIntervalInSeconds = 5)
    {
        
    }
    public Task<bool> InvalidateCacheAsync(Type[] types)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types) where T : class
    {
        throw new NotImplementedException();
    }

    public Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        throw new NotImplementedException();
    }
}