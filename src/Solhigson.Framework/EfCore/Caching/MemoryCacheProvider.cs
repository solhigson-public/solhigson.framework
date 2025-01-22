using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Timers;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Utilities;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore.Caching;

public class MemoryCacheProvider : CacheProviderBase
{
    private static Timer? _timer;
    private static MemoryCache DefaultMemoryCache { get; } = new("Solhigson::EfCore::Memory::Cache::Manager");
    private static readonly ConcurrentDictionary<string, EntityChangeTrackerHandler> ChangeTrackers = new();
    private readonly string _cacheKey;
    public event EventHandler? OnTableChangeTimerElapsed;

    internal MemoryCacheProvider(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440,
        int changeTrackerTimerIntervalInSeconds = 5) : base(redis, prefix, expirationInMinutes)
    {
        _cacheKey = $"{prefix}memory.tracker";
        StartCacheTimer(changeTrackerTimerIntervalInSeconds);
    }

    private void StartCacheTimer(int changeTrackerTimerIntervalInSeconds)
    {
        _timer = new Timer(changeTrackerTimerIntervalInSeconds * 1000);
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var changeTrackers = GetEntityChangeTrackersAsync().Result;
        if (!changeTrackers.HasData())
        {
            return;
        }

        OnTableChangeTimerElapsed?.Invoke(null, new EntityChangeTrackerEventArgs(changeTrackers));
    }


    public override async Task<bool> InvalidateCacheAsync(Type[] types)
    {
        var trackerInfo = await GetEntityChangeTrackersAsync();
        foreach (var type in types)
        {
            var key = EfCoreCacheManager.GetTypeName(type);
            if (trackerInfo.TryGetValue(key, out var changeId))
            {
                trackerInfo[key] = ++changeId;
            }
            else
            {
                trackerInfo.Add(key, changeId);
            }
        }

        _ = Database.StringSetAsync(_cacheKey, trackerInfo.SerializeToJson(), TimeSpan.FromMinutes(ExpirationInMinutes));
        return true;
    }

    public override async Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types)
    {
        var policy = new CacheItemPolicy();
        var changeMonitor = new EntityChangeMonitor(GetEntityChangeTrackerHandler(types));
        policy.ChangeMonitors.Add(changeMonitor);

        DefaultMemoryCache.Set(cacheKey, data, policy);
        return await Task.FromResult(true);
    }

    public override async Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        var responseInfo = new ResponseInfo<T?>();
        var result = DefaultMemoryCache.Get(cacheKey) is T entry 
            ? responseInfo.Success(entry) 
            : responseInfo.Fail();
        return await Task.FromResult(result);
    }

    internal async Task<int> GetEntityChangeTrackerChangeId(string entityName)
    {
        var trackerInfo = await GetEntityChangeTrackersAsync();
        trackerInfo.TryGetValue(entityName, out var changeId);
        return changeId;
    }

    private EntityChangeTrackerHandler GetEntityChangeTrackerHandler(Type[] types)
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

    internal static string Flatten(Type[]? iCacheEntityTypes)
    {
        return !iCacheEntityTypes.HasData() 
            ? string.Empty 
            : Flatten(iCacheEntityTypes!.Select(EfCoreCacheManager.GetTypeName));
    }

    internal static string Flatten(IEnumerable<string> names)
    {
        var result = "";
        foreach (var name in names)
        {
            result = string.IsNullOrEmpty(result)
                ? name
                : $"{result}-{name}";
        }

        return result;
    }

    private async Task<Dictionary<string, short>> GetEntityChangeTrackersAsync()
    {
        var resp = await Database.StringGetAsync(_cacheKey);
        string? json = resp;
        return json.DeserializeFromJson<Dictionary<string, short>>() ?? new Dictionary<string, short>();
    }
}