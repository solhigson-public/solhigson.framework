using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;
using Solhigson.Utilities.Dto;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore.Caching;

public abstract class CacheProviderBase : ICacheProvider
{
    private readonly IDatabase? _database;
    protected readonly int ExpirationInMinutes;
    private readonly string _prefix;
    private readonly Func<IConnectionMultiplexer>? _connectionMultiplexerFactory;

    protected CacheProviderBase(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440)
    {
        _prefix = prefix;
        _database = redis.GetDatabase();
        ExpirationInMinutes = expirationInMinutes;
    }

    protected CacheProviderBase(Func<IConnectionMultiplexer> connectionMultiplexerFactor, string prefix, int expirationInMinutes = 1440)
    {
        _prefix = prefix;
        _connectionMultiplexerFactory = connectionMultiplexerFactor;
        ExpirationInMinutes = expirationInMinutes;
    }

    protected IDatabase GetDatabase()
    {
        return _database ?? _connectionMultiplexerFactory!().GetDatabase();
    }
    
    protected string GetTagKey(Type type)
    {
        return _prefix + EfCoreCacheManager.GetTypeName(type);
    }


    public abstract Task<bool> InvalidateCacheAsync(Type[] types, CancellationToken cancellationToken = default);

    public abstract Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types, CancellationToken cancellationToken = default) where T : class;

    public abstract Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class;
}