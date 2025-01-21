using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Utilities;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore.Caching;

public class MemoryCacheProvider : CacheProviderBase
{
    private static readonly ConcurrentDictionary<string, EntityChangeTrackerHandler> ChangeTrackers = new();
    private readonly string _cacheKey;
    public event EventHandler? OnTableChangeTimerElapsed;
    internal MemoryCacheProvider(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440,
        int changeTrackerTimerIntervalInSeconds = 5) : base(redis, prefix, expirationInMinutes)
    {
        _cacheKey = $"{prefix}memory.tracker";
    }

    public override async Task<bool> InvalidateCacheAsync(Type[] types)
    {
        var tran = Database.CreateTransaction();
        
        var resp = await tran.StringGetAsync(_cacheKey);
        string? json = resp;
        var trackerInfo = json.DeserializeFromJson<Dictionary<string, int>>() ?? new Dictionary<string, int>();
        foreach (var type in types)
        {
            var key = EfCoreCacheManager.GetTypeName(type);
            if (trackerInfo.TryGetValue(key, out var changeId))
            {
                trackerInfo[key] = changeId + 1;
            }
            else
            {
                trackerInfo[key] = 1;
            }
        }
        _ = tran.StringSetAsync(_cacheKey, trackerInfo.SerializeToJson());
        
        return await tran.ExecuteAsync();
    }

    public override async Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types)
    {
        throw new NotImplementedException();
    }

    public override async Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        throw new NotImplementedException();
    }

    internal async Task<short> GetEntityChangeTrackerChangeId(string tableName)
    {
        throw new NotImplementedException();
    }
    
    private EntityChangeTrackerHandler GetEntityChangeTrackerHandler(IReadOnlyCollection<Type> types)
    {
        var changeTrackerKey = Flatten(types);
        if (ChangeTrackers.TryGetValue(changeTrackerKey, out var entityChangeTrackerHandler))
        {
            return entityChangeTrackerHandler;
        }

        entityChangeTrackerHandler = new EntityChangeTrackerHandler(this, types);
        try
        {
            ChangeTrackers.TryAdd(changeTrackerKey, entityChangeTrackerHandler);
        }
        catch (Exception e)
        {
            this.LogError(e);
        }

        return entityChangeTrackerHandler;
    }
    
    private static string Flatten(IEnumerable<Type> iCacheEntityTypes)
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