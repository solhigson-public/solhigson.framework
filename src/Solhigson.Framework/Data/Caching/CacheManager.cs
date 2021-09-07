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

namespace Solhigson.Framework.Data.Caching
{
    public static class CacheManager
    {
        private static Timer _timer;

        private static string _connectionString;
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();
        private static int _cacheDependencyChangeTrackerTimerIntervalMilliseconds;
        private static int _cacheExpirationPeriodMinutes;
        public static event EventHandler OnTableChangeTimerElapsed;
        private static readonly ConcurrentDictionary<string, TableChangeTracker> ChangeMonitors = new();

        private static MemoryCache DefaultMemoryCache { get; } = new ("Solhigson::Data::Cache::Manager");
        private static ConcurrentBag<string> CacheKeys { get; } = new ();

        internal static void Initialize(string connectionString,
            int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
            int cacheExpirationPeriodMinutes = 1440, Assembly databaseModelsAssembly = null)
        {
            try
            {
                _connectionString = connectionString;
                _cacheExpirationPeriodMinutes = cacheExpirationPeriodMinutes;
                _cacheDependencyChangeTrackerTimerIntervalMilliseconds =
                    cacheDependencyChangeTrackerTimerIntervalMilliseconds;
                ScriptsManager.SetUpDatabaseObjects(databaseModelsAssembly, connectionString);
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
                return await AdoNetUtils.GetListAsync<ChangeTrackerDto>
                    (_connectionString, $"[{ScriptsManager.CacheChangeTrackerInfo.GetAllChangeTrackerSpName}]", isStoredProcedure: true);
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
                return await AdoNetUtils.GetSingleOrDefaultAsync<short>
                (_connectionString,
                    $"EXEC [{ScriptsManager.CacheChangeTrackerInfo.GetTableChangeTrackerSpName}]  {ScriptsManager.GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)} = N'{tableName}'");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to get Cache Change Tracker Id");
            }

            return 0;
        }

        public static void AddToCache(string key, object value, Type type)
        {
            if (string.IsNullOrWhiteSpace(key) || type is null)
            {
                return;
            }

            if (!typeof(ICachedEntity).IsAssignableFrom(type))
            {
                Logger.Warn(
                    $"Data of type: [{type}] will not be cached as it does not inherit from [{nameof(ICachedEntity)}]");
                return;
            }

            InsertItem(key, value, new TableChangeMonitor(GetTableChangeTracker(type)));
        }

        public static void InsertItem(string key, object value, ChangeMonitor changeMonitor = null)
        {
            if (string.IsNullOrWhiteSpace(key))// || value == null)
            {
                changeMonitor?.Dispose();
                return;
            }

            var entry = new CustomCacheEntry{ Value = value };

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
                CacheKeys.Add(key);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Adding item to cache", value);
            }
        }

        internal static CustomCacheEntry GetFromCache<T>(string key) where T : class
        {
            return DefaultMemoryCache.Get(key) as CustomCacheEntry;
        }

        internal static TableChangeTracker GetTableChangeTracker(Type type)
        {
            return GetTableChangeTracker(ScriptsManager.GetTableName(type));
        }


        private static TableChangeTracker GetTableChangeTracker(string tableName)
        {
            if (ChangeMonitors.TryGetValue(tableName, out var tableChangeTracker))
            {
                return tableChangeTracker;
            }

            tableChangeTracker = new TableChangeTracker(tableName);
            try
            {
                ChangeMonitors.TryAdd(tableName, tableChangeTracker);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return tableChangeTracker;
        }


    }
    
    internal class CustomCacheEntry
    {
        public object Value { get; set; }
    }
}