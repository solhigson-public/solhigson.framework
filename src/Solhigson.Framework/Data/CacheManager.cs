using System;
using System.Collections.Concurrent;
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
        

        private static MemoryCache DefaultMemoryCache { get; } = new MemoryCache("Fp::Data::Cache::Manager");
        private static ConcurrentBag<string> CacheKeys { get; } = new ConcurrentBag<string>();

        internal static void Initialize(string connectionString, int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
            int cacheExpirationPeriodMinutes = 1440)
        {
            _connectionString = connectionString;
            _cacheExpirationPeriodMinutes = cacheExpirationPeriodMinutes;
            _cacheDependencyChangeTrackerTimerIntervalMilliseconds = cacheDependencyChangeTrackerTimerIntervalMilliseconds;
            InitializeCacheChangeTracker();
            StartCacheTimer(_cacheDependencyChangeTrackerTimerIntervalMilliseconds);
        }

        private static void InitializeCacheChangeTracker()
        {
            var sBuilder = new StringBuilder();
            var getChangeTrackerBuilder = new StringBuilder();
            var updateChangeTrackerBuilder = new StringBuilder();
            sBuilder.Append("IF OBJECT_ID(N'[__FpCacheChangeTracker]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append("CREATE TABLE [__FpCacheChangeTracker] ( ");
            sBuilder.Append("[ChangeId] int NOT NULL ");
            sBuilder.Append("CONSTRAINT [PK__FpCacheChangeTracker] PRIMARY KEY ([ChangeId])); ");
            sBuilder.Append("INSERT INTO [__FpCacheChangeTracker] (ChangeId) VALUES (1); END;");

            sBuilder.Append("IF OBJECT_ID(N'[__FpUsp_UpdateChangeTracker]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append("DROP PROCEDURE [__FpUsp_UpdateChangeTracker] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append("IF OBJECT_ID(N'[__FpUsp_GetChangeTrackerId]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append("DROP PROCEDURE [__FpUsp_GetChangeTrackerId] ");
            sBuilder.Append("END;");

            getChangeTrackerBuilder.Append("CREATE PROCEDURE [__FpUsp_GetChangeTrackerId] ");
            getChangeTrackerBuilder.Append("AS ");
            getChangeTrackerBuilder.Append("SELECT ChangeId from [__FpCacheChangeTracker] (NOLOCK) ");

            updateChangeTrackerBuilder.Append("CREATE PROCEDURE [__FpUsp_UpdateChangeTracker] ");
            updateChangeTrackerBuilder.Append("AS ");
            updateChangeTrackerBuilder.Append("DECLARE @id INT ");
            updateChangeTrackerBuilder.Append("select @id = ChangeId from [__FpCacheChangeTracker] (NOLOCK) ");
            updateChangeTrackerBuilder.Append("IF(@id > 1000) ");
            updateChangeTrackerBuilder.Append("BEGIN ");
            updateChangeTrackerBuilder.Append("SET @id = 1 ");
            updateChangeTrackerBuilder.Append("END ");
            updateChangeTrackerBuilder.Append("UPDATE dbo.[__FpCacheChangeTracker] SET ChangeId = @id + 1 ");

            AdoNetUtils.ExecuteNonQueryAsync(_connectionString, sBuilder.ToString()).Wait();
            AdoNetUtils.ExecuteNonQueryAsync(_connectionString, updateChangeTrackerBuilder.ToString()).Wait();
            AdoNetUtils.ExecuteNonQueryAsync(_connectionString, getChangeTrackerBuilder.ToString()).Wait();
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
                return AdoNetUtils.ExecuteScalarAsync<int>(_connectionString, "[__FpUsp_GetChangeTrackerId]",
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
                await AdoNetUtils.ExecuteNonQueryAsync(_connectionString, "[__FpUsp_UpdateChangeTracker]");
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