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


        private static readonly ConcurrentDictionary<string, TableChangeTracker> ChangeMonitors =
            new ConcurrentDictionary<string, TableChangeTracker>();


        private static MemoryCache DefaultMemoryCache { get; } = new MemoryCache("Solhigson::Data::Cache::Manager");
        private static ConcurrentBag<string> CacheKeys { get; } = new ConcurrentBag<string>();

        internal const string AppSettingsTableName = "__SolhigsonAppSettings";
        internal const string AppSettingsTableNameColumn = "Name";
        internal const string AppSettingsTableValueColumn = "Value";
        internal const string AppSettingsTableIdColumn = "Id";
        private const string ChangeTrackerTableName = "__SolhigsonCacheChangeTracker";
        private const string UpdateChangeTrackerSpName = "__SolhigsonUpdateChangeTracker";
        private const string GetAllChangeTrackerSpName = "__SolhigsonGetAllChangeTrackerIds";
        private const string GetTableChangeTrackerSpName = "__SolhigsonGetTableChangeTrackerId";
        private const string TableNameColumnName = "TableName";
        private const string ChangeIdColumnName = "ChangeId";

        private static string GetParameterName(string tableName)
        {
            return $"@{tableName}";
        }

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
                InitializeCacheChangeTracker(databaseModelsAssembly);
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

        private static void InitializeCacheChangeTracker(Assembly databaseModelsAssembly)
        {
            var sBuilder = new StringBuilder();
            var getAllChangeTrackerBuilder = new StringBuilder();
            var getTableChangeTrackerBuilder = new StringBuilder();
            var updateChangeTrackerBuilder = new StringBuilder();

            //Clean up cache monitor table and all triggers, for entities that might have been removed as ICacheEntity
            var cleanUpScript = $@"DECLARE @sql NVARCHAR(MAX) = N'Delete from [{ChangeTrackerTableName}];';
                SELECT @sql += 
                    N'DROP TRIGGER ' + 
                    QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + 
                    QUOTENAME(t.name) + N'; ' + NCHAR(13)
                FROM sys.triggers AS t
                WHERE t.is_ms_shipped = 0
                  AND t.parent_class_desc = N'OBJECT_OR_COLUMN';

                exec (N'' + @sql + N'');
                ";

            sBuilder.Append($"IF OBJECT_ID(N'[{ChangeTrackerTableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{ChangeTrackerTableName}] ( ");
            sBuilder.Append($"{TableNameColumnName} VARCHAR(255) NOT NULL, {ChangeIdColumnName} SMALLINT NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{ChangeTrackerTableName}] PRIMARY KEY ([{TableNameColumnName}])); END;");

            sBuilder.Append($"IF OBJECT_ID(N'[{AppSettingsTableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{AppSettingsTableName}] ( ");
            sBuilder.Append($"{AppSettingsTableIdColumn} INT IDENTITY(1,1) NOT NULL, {AppSettingsTableNameColumn} VARCHAR(255) NOT NULL, {AppSettingsTableValueColumn} VARCHAR(MAX) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{AppSettingsTableName}] PRIMARY KEY ([{AppSettingsTableIdColumn}])); ");//END;");

            sBuilder.Append($"CREATE UNIQUE NONCLUSTERED INDEX [UIX_{AppSettingsTableName}_ON_Name] ");
            sBuilder.Append($"ON [dbo].[{AppSettingsTableName}] ");
            sBuilder.Append($"( [Name] ASC ); END; ");

            sBuilder.Append($"IF OBJECT_ID(N'[{UpdateChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{UpdateChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{GetAllChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{GetAllChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{GetTableChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{GetTableChangeTrackerSpName}] ");
            sBuilder.Append("END;");

            getAllChangeTrackerBuilder.Append($"CREATE PROCEDURE [{GetAllChangeTrackerSpName}] ");
            getAllChangeTrackerBuilder.Append("AS ");
            getAllChangeTrackerBuilder.Append(
                $"SELECT [{TableNameColumnName}], [{ChangeIdColumnName}] from [{ChangeTrackerTableName}] (NOLOCK)");

            getTableChangeTrackerBuilder.Append(
                $"CREATE PROCEDURE [{GetTableChangeTrackerSpName}] ({GetParameterName(TableNameColumnName)} VARCHAR(255)) ");
            getTableChangeTrackerBuilder.Append("AS ");
            getTableChangeTrackerBuilder.Append(
                $"SELECT [{ChangeIdColumnName}] from [{ChangeTrackerTableName}] (NOLOCK) WHERE [{TableNameColumnName}] = {GetParameterName(TableNameColumnName)}");

            updateChangeTrackerBuilder.Append(
                $"CREATE PROCEDURE [{UpdateChangeTrackerSpName}] ({GetParameterName(TableNameColumnName)} VARCHAR(255)) ");
            updateChangeTrackerBuilder.Append("AS ");
            updateChangeTrackerBuilder.Append($"DECLARE {GetParameterName(ChangeIdColumnName)} INT ");
            updateChangeTrackerBuilder.Append(
                $"SELECT {GetParameterName(ChangeIdColumnName)} = [{ChangeIdColumnName}] FROM [{ChangeTrackerTableName}] (NOLOCK) WHERE [{TableNameColumnName}] = {GetParameterName(TableNameColumnName)} ");
            updateChangeTrackerBuilder.Append($"IF({GetParameterName(ChangeIdColumnName)} IS NULL) " +
                                              $"BEGIN " +
                                              $"INSERT INTO [{ChangeTrackerTableName}] ([{TableNameColumnName}], [{ChangeIdColumnName}]) VALUES ({GetParameterName(TableNameColumnName)}, 1) RETURN 1 " +
                                              $"END ");
            updateChangeTrackerBuilder.Append($"IF({GetParameterName(ChangeIdColumnName)} > 1000) ");
            updateChangeTrackerBuilder.Append($"BEGIN " +
                                              $"SET {GetParameterName(ChangeIdColumnName)} = 1 " +
                                              $"END ");
            updateChangeTrackerBuilder.Append(
                $"UPDATE dbo.[{ChangeTrackerTableName}] SET [{ChangeIdColumnName}] = {GetParameterName(ChangeIdColumnName)} + 1 WHERE [{TableNameColumnName}] = {GetParameterName(TableNameColumnName)}");

            var dbTriggerCommands = new List<StringBuilder>();
            dbTriggerCommands.AddRange(GetCacheTrackerTriggerCommands(typeof(AppSetting)));
            if (databaseModelsAssembly != null)
            {
                var cachedEntityType = typeof(ICachedEntity);
                foreach (var type in databaseModelsAssembly.GetTypes()
                    .Where(t => cachedEntityType.IsAssignableFrom(t) && !t.IsInterface))
                {
                    dbTriggerCommands.AddRange(GetCacheTrackerTriggerCommands(type));
                }
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                using var cmd = new SqlCommand(sBuilder.ToString(), conn);
                conn.OpenAsync();
                cmd.ExecuteNonQuery();
                cmd.CommandText = cleanUpScript;
                cmd.ExecuteNonQuery();
                cmd.CommandText = updateChangeTrackerBuilder.ToString();
                cmd.ExecuteNonQuery();
                cmd.CommandText = getAllChangeTrackerBuilder.ToString();
                cmd.ExecuteNonQuery();
                cmd.CommandText = getTableChangeTrackerBuilder.ToString();
                cmd.ExecuteNonQuery();
                foreach (var command in dbTriggerCommands)
                {
                    cmd.CommandText = command.ToString();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string GetTableName(Type entityType, bool forQuery = false)
        {
            var tableAttribute = entityType.GetAttribute<TableAttribute>();

            var tableName = tableAttribute?.Name ?? entityType.Name;
            var schema = tableAttribute?.Schema;
            if (string.IsNullOrWhiteSpace(schema))
            {
                return forQuery ? $"[{tableName}]" : tableName;
            }

            return forQuery ? $"[{schema}].[{tableName}]" : $"{schema}_{tableName}";
        }

        private static string GetTriggerName(Type entityType)
        {
            var triggerName = $"Solhigson_UTrig_{GetTableName(entityType)}_UpdateChangeTracker";

            var schema = entityType.GetAttribute<TableAttribute>()?.Schema;
            return string.IsNullOrWhiteSpace(schema)
                ? $"[{triggerName}]"
                : $"[{schema}].[{triggerName}]";
        }

        private static IEnumerable<StringBuilder> GetCacheTrackerTriggerCommands(Type entityType)
        {
            var list = new List<StringBuilder>();
            var triggerName = GetTriggerName(entityType);

            var deleteScriptBuilder = new StringBuilder();
            deleteScriptBuilder.Append($"IF OBJECT_ID(N'{triggerName}') IS NOT NULL ");
            deleteScriptBuilder.Append("BEGIN ");
            deleteScriptBuilder.Append($"DROP TRIGGER {triggerName} ");
            deleteScriptBuilder.Append("END;");

            var createTriggerScriptBuilder = new StringBuilder();
            createTriggerScriptBuilder.Append(
                $"CREATE TRIGGER {triggerName} ON {GetTableName(entityType, true)} AFTER INSERT, DELETE, UPDATE AS ");
            createTriggerScriptBuilder.Append(
                $"BEGIN TRY SET NOCOUNT ON; EXEC [{UpdateChangeTrackerSpName}] {GetParameterName(TableNameColumnName)} = N'{GetTableName(entityType)}' END TRY BEGIN CATCH END CATCH");

            list.Add(deleteScriptBuilder);
            list.Add(createTriggerScriptBuilder);
            return list;
        }
        
        private static async Task<List<ChangeTrackerDto>> GetAllChangeTrackerIds()
        {
            try
            {
                return await AdoNetUtils.GetListAsync<ChangeTrackerDto>
                    (_connectionString, $"[{GetAllChangeTrackerSpName}]", isStoredProcedure: true);
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
                    $"EXEC [{GetTableChangeTrackerSpName}]  {GetParameterName(TableNameColumnName)} = N'{tableName}'");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to get Cache Change Tracker Id");
            }

            return 0;
        }

        public static void AddToCache(string key, object value, Type type)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null || type is null)
            {
                return;
            }

            if (!typeof(ICachedEntity).IsAssignableFrom(type))
            {
                Logger.Warn(
                    $"Data of type: [{value.GetType()}] will not be cached as it does not inherit from [{nameof(ICachedEntity)}]");
                return;
            }

            InsertItem(key, value, new TableChangeMonitor(GetTableChangeTracker(type)));
        }

        public static void InsertItem(string key, object value, ChangeMonitor changeMonitor = null)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                changeMonitor?.Dispose();
                return;
            }

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

                DefaultMemoryCache.Set(key, value, policy);
                CacheKeys.Add(key);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Adding item to cache", value);
            }
        }

        public static T GetFromCache<T>(string key) where T : class
        {
            return GetItem(key) as T;
        }

        private static object GetItem(string key)
        {
            return DefaultMemoryCache.Get(key);
        }

        internal static TableChangeTracker GetTableChangeTracker(Type type)
        {
            return GetTableChangeTracker(GetTableName(type));
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
}