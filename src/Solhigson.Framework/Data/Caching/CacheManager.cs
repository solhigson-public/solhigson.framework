using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Data.SqlClient;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.Data.Caching;

public static class CacheManager
{
    private static Timer _timer;

    private static string _connectionString;
    private static readonly LogWrapper Logger = new LogWrapper(typeof(CacheManager).FullName);
    private static int _cacheDependencyChangeTrackerTimerIntervalMilliseconds;
    private static int _cacheExpirationPeriodMinutes;
    public static event EventHandler OnTableChangeTimerElapsed;
    private static readonly ConcurrentDictionary<string, TableChangeTracker> ChangeTrackers = new();

    private static MemoryCache DefaultMemoryCache { get; } = new ("Solhigson::Data::Cache::Manager");
    //private static ConcurrentBag<string> CacheKeys { get; } = new ();

    internal static void Initialize(string connectionString,
        int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
        int cacheExpirationPeriodMinutes = 1440, Assembly dbContextAssembly = null)
    {
        try
        {
            _connectionString = connectionString;
            _cacheExpirationPeriodMinutes = cacheExpirationPeriodMinutes;
            _cacheDependencyChangeTrackerTimerIntervalMilliseconds =
                cacheDependencyChangeTrackerTimerIntervalMilliseconds;
            ScriptsManager.SetUpDatabaseObjects(dbContextAssembly, connectionString);
            StartCacheTimer();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    private static void StartCacheTimer()
    {
        _timer = new Timer(_cacheDependencyChangeTrackerTimerIntervalMilliseconds);
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        var changes = GetAllChangeTrackerIds().Result;
        if (changes != null)
        {
            var changeTrackers = changes.ToDictionary(changeTracker => changeTracker.TableName,
                changeTracker => changeTracker.ChangeId);
            OnTableChangeTimerElapsed?.Invoke(null, new ChangeTrackerEventArgs(changeTrackers));
        }
    }

       
    private static async Task<List<ChangeTrackerDto>> GetAllChangeTrackerIds()
    {
        try
        {
            return await AdoNetUtils.ExecuteListAsync<ChangeTrackerDto>
                (_connectionString, $"[{ScriptsManager.CacheChangeTrackerInfo.GetAllChangeTrackerSpName}]");
        }
        catch (Exception e)
        {
            Logger.Error(e, "Unable to get Cache Change Tracker Id");
        }

        return null;
    }

    internal static async Task<short> GetTableChangeTrackerId(string tableName)
    {
        try
        {
            return await AdoNetUtils.ExecuteSingleOrDefaultAsync<short>
            (_connectionString,
                $"EXEC [{ScriptsManager.CacheChangeTrackerInfo.GetTableChangeTrackerSpName}]  {ScriptsManager.GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)} = N'{tableName}'",
                isStoredProcedure: false);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Unable to get Cache Change Tracker Id");
        }

        return 0;
    }

    internal static void AddToCache(string key, object value, IList<Type> types)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (types == null || !types.Any())
        {
            InsertItem(key, value);
            return;
        }

        InsertItem(key, value, new TableChangeMonitor(GetTableChangeTracker(types)));
    }

    internal static IList<Type> GetValidICacheEntityTypes(params Type [] types)
    {
        var validTypes = new List<Type>();
        if (types is not null && types.Any())
        {
            validTypes.AddRange(types.Where(type => typeof(ICachedEntity).IsAssignableFrom(type)));
        }
        return validTypes;
    }

    public static void InsertItem(string key, object value, ChangeMonitor changeMonitor = null)
    {
        if (string.IsNullOrWhiteSpace(key))// || value == null)
        {
            changeMonitor?.Dispose();
            return;
        }

        var entry = new CustomCacheEntry{ Value = value };
        DefaultMemoryCache.Remove(key);

        try
        {
            var policy = new CacheItemPolicy();
            if (changeMonitor != null)
            {
                policy.ChangeMonitors.Add(changeMonitor);
            }
            else
            {
                policy.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(_cacheExpirationPeriodMinutes);
            }

            DefaultMemoryCache.Set(key, entry, policy);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Adding item to cache", value);
        }
    }

    internal static CustomCacheEntry GetFromCache(string key)// where T : class
    {
        return DefaultMemoryCache.Get(key) as CustomCacheEntry;
    }

    private static TableChangeTracker GetTableChangeTracker(IEnumerable<Type> types)
    {
        return GetTableChangeTracker(types.Select(t => ScriptsManager.GetTableName(t)).ToList());
    }


    private static TableChangeTracker GetTableChangeTracker(IReadOnlyCollection<string> tableNames)
    {
        var changeTrackerKey = Flatten(tableNames);
        if (ChangeTrackers.TryGetValue(changeTrackerKey, out var tableChangeTracker))
        {
            return tableChangeTracker;
        }

        tableChangeTracker = new TableChangeTracker(tableNames);
        try
        {
            ChangeTrackers.TryAdd(changeTrackerKey, tableChangeTracker);
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }

        return tableChangeTracker;
    }

    internal static string Flatten(IEnumerable<string> tableNames)
    {
        var result = "";
        foreach (var tableName in tableNames)
        {
            if (string.IsNullOrEmpty(result))
            {
                result = tableName;
            }
            else
            {
                result = $"{result}-{tableName}";
            }
        }
        return result;
    }

}
    
internal class CustomCacheEntry
{
    public object Value { get; set; }
}