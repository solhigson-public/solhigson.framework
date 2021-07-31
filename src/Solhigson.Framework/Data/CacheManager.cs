using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Data
{
    public static class CacheManager
    {
        private static string _connectionString;
        //private static SolhigsonServicesWrapper _servicesWrapper;
        private static Timer _timer;
        private static int _currentChangeTrackerId;
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();
        private static int _cacheDependencyChangeTrackerTimerIntervalMilliseconds;
        private static int _cacheExpirationPeriodMinutes;
        

        private static MemoryCache DefaultMemoryCache { get; } = new MemoryCache("Solhigson::Data::Cache::Manager");
        private static ConcurrentBag<string> CacheKeys { get; } = new ConcurrentBag<string>();
        
        const string changeTrackerTableName = "Solhigson_CacheChangeTracker";
        const string updateChangeTrackerSpName = "Solhigson_Usp_UpdateChangeTracker";
        const string getChangeTrackerSpName = "Solhigson_Usp_GetChangeTrackerId";

        internal static void Initialize(string connectionString, int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
            int cacheExpirationPeriodMinutes = 1440, Assembly databaseModelsAssembly = null)
        {
            _connectionString = connectionString;
            _cacheExpirationPeriodMinutes = cacheExpirationPeriodMinutes;
            _cacheDependencyChangeTrackerTimerIntervalMilliseconds = cacheDependencyChangeTrackerTimerIntervalMilliseconds;
            InitializeCacheChangeTracker(databaseModelsAssembly);
            StartCacheTimer(_cacheDependencyChangeTrackerTimerIntervalMilliseconds);
        }

        private static void InitializeCacheChangeTracker(Assembly databaseModelsAssembly)
        {
            var sBuilder = new StringBuilder();
            var getChangeTrackerBuilder = new StringBuilder();
            var updateChangeTrackerBuilder = new StringBuilder();
            sBuilder.Append($"IF OBJECT_ID(N'[{changeTrackerTableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{changeTrackerTableName}] ( ");
            sBuilder.Append("[ChangeId] int NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{changeTrackerTableName}] PRIMARY KEY ([ChangeId])); ");
            sBuilder.Append($"INSERT INTO [{changeTrackerTableName}] (ChangeId) VALUES (1); END;");

            sBuilder.Append($"IF OBJECT_ID(N'[{updateChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{updateChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{getChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{getChangeTrackerSpName}] ");
            sBuilder.Append("END;");

            getChangeTrackerBuilder.Append($"CREATE PROCEDURE [{getChangeTrackerSpName}] ");
            getChangeTrackerBuilder.Append("AS ");
            getChangeTrackerBuilder.Append($"SELECT ChangeId from [{changeTrackerTableName}] (NOLOCK) ");

            updateChangeTrackerBuilder.Append($"CREATE PROCEDURE [{updateChangeTrackerSpName}] ");
            updateChangeTrackerBuilder.Append("AS ");
            updateChangeTrackerBuilder.Append("DECLARE @id INT ");
            updateChangeTrackerBuilder.Append($"select @id = ChangeId from [{changeTrackerTableName}] (NOLOCK) ");
            updateChangeTrackerBuilder.Append("IF(@id > 1000) ");
            updateChangeTrackerBuilder.Append("BEGIN ");
            updateChangeTrackerBuilder.Append("SET @id = 1 ");
            updateChangeTrackerBuilder.Append("END ");
            updateChangeTrackerBuilder.Append($"UPDATE dbo.[{changeTrackerTableName}] SET ChangeId = @id + 1 ");

            AdoNetUtils.ExecuteNonQueryAsync(_connectionString, sBuilder.ToString()).Wait();
            AdoNetUtils.ExecuteNonQueryAsync(_connectionString, updateChangeTrackerBuilder.ToString()).Wait();
            AdoNetUtils.ExecuteNonQueryAsync(_connectionString, getChangeTrackerBuilder.ToString()).Wait();

            if (databaseModelsAssembly == null)
            {
                return;
            }

            var cachedEntityType = typeof(ICachedEntity);
            foreach (var type in databaseModelsAssembly.GetTypes()
                .Where(t => cachedEntityType.IsAssignableFrom(t) && !t.IsInterface))
            {
                AddCacheTrackerTrigger(type);
            }
        }

        private static void AddCacheTrackerTrigger(Type entityType)
        {
            string tableName = null;
            try
            {
                var tableAttribute = entityType.GetAttribute<TableAttribute>();
                tableName = tableAttribute?.Name ?? entityType.Name;
                var triggerName = $"Solhigson_UTrig_{tableName}_UpdateChangeTracker";

                var deleteScriptBuilder = new StringBuilder();
                deleteScriptBuilder.Append($"IF OBJECT_ID(N'[{triggerName}]') IS NOT NULL ");
                deleteScriptBuilder.Append("BEGIN ");
                deleteScriptBuilder.Append($"DROP PROCEDURE [{triggerName}] ");
                deleteScriptBuilder.Append("END;");

                var createTriggerScriptBuilder = new StringBuilder();
                createTriggerScriptBuilder.Append($"CREATE TRIGGER [{triggerName}] ON [{tableName}] AFTER INSERT, DELETE, UPDATE AS ");
                createTriggerScriptBuilder.Append($"BEGIN SET NOCOUNT ON; EXEC [{updateChangeTrackerSpName}] END");
            
                AdoNetUtils.ExecuteNonQueryAsync(_connectionString, deleteScriptBuilder.ToString()).Wait();
                AdoNetUtils.ExecuteNonQueryAsync(_connectionString, createTriggerScriptBuilder.ToString()).Wait();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Creating Cache Trigger on {entityType.Name} => {tableName}");
            }
        }

        private static void StartCacheTimer(int interval)
        {
            if (interval > 20000 || interval < 5000)
            {
                interval = 5000;
            }

            _currentChangeTrackerId = GetChangeId();
            _timer = new Timer(interval);

            _timer.Start();
            _timer.Elapsed += TimerOnElapsed;
        }

        private static int GetChangeId()
        {
            try
            {
                return AdoNetUtils.ExecuteScalarAsync<int>(_connectionString, $"[{getChangeTrackerSpName}]",
                    isStoredProcedure: true).Result;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to get Cache Change Tracker Id");
            }

            return 0;
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var changeId = GetChangeId();
            if (changeId == _currentChangeTrackerId)
            {
                return;
            }

            _currentChangeTrackerId = changeId;
            foreach (var key in CacheKeys)
            {
                DefaultMemoryCache.Remove(key);
            }
        }

        public static async Task<bool> ResyncCache()
        {
            try
            {
                await AdoNetUtils.ExecuteNonQueryAsync(_connectionString, $"[{updateChangeTrackerSpName}]");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return false;
        }

        public static void InsertItem(string key, object value, ChangeMonitor changeMonitor = null)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return;
            }

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(_cacheExpirationPeriodMinutes),
            };

            if (changeMonitor != null)
            {
                policy.ChangeMonitors.Add(changeMonitor);
            }

            DefaultMemoryCache.Set(key, value, policy);
            CacheKeys.Add(key);
        }

        public static T GetFromCache<T>(string key) where T : class
        {
            return GetItem(key) as T;
        }

        private static object GetItem(string key)
        {
            return DefaultMemoryCache.Get(key);
        }
    }
}