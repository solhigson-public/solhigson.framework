using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.Data
{
    public static class ScriptsManager
    {
        internal static void InitializeCacheChangeTracker(Assembly databaseModelsAssembly)
        {
            var sBuilder = new StringBuilder();
            var getAllChangeTrackerBuilder = new StringBuilder();
            var getTableChangeTrackerBuilder = new StringBuilder();
            var updateChangeTrackerBuilder = new StringBuilder();

            //Clean up cache monitor table and all triggers, for entities that might have been removed as ICacheEntity
            var cleanUpScript = $@"DECLARE @sql NVARCHAR(MAX) = N'Delete from [{ScriptsManager.CacheChangeTrackerInfo.TableName}];';
                SELECT @sql += 
                    N'DROP TRIGGER ' + 
                    QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + 
                    QUOTENAME(t.name) + N'; ' + NCHAR(13)
                FROM sys.triggers AS t
                WHERE t.is_ms_shipped = 0
                  AND t.parent_class_desc = N'OBJECT_OR_COLUMN';

                exec (N'' + @sql + N'');
                ";

            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.CacheChangeTrackerInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{ScriptsManager.CacheChangeTrackerInfo.TableName}] ( ");
            sBuilder.Append($"{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn} VARCHAR(255) NOT NULL, {ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn} SMALLINT NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{ScriptsManager.CacheChangeTrackerInfo.TableName}] PRIMARY KEY ([{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn}])); END;");

            #region AppSettings Table
            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.AppSettingInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{ScriptsManager.AppSettingInfo.TableName}] ( ");
            sBuilder.Append($"{ScriptsManager.AppSettingInfo.IdColumn} INT IDENTITY(1,1) NOT NULL, {ScriptsManager.AppSettingInfo.NameColumn} VARCHAR(255) NOT NULL, {ScriptsManager.AppSettingInfo.ValueColumn} VARCHAR(MAX) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{ScriptsManager.AppSettingInfo.TableName}] PRIMARY KEY ([{ScriptsManager.AppSettingInfo.IdColumn}])); ");//END;");

            sBuilder.Append($"CREATE UNIQUE NONCLUSTERED INDEX [UIX_{ScriptsManager.AppSettingInfo.TableName}_ON_{ScriptsManager.AppSettingInfo.NameColumn}] ");
            sBuilder.Append($"ON [dbo].[{ScriptsManager.AppSettingInfo.TableName}] ");
            sBuilder.Append($"( [{ScriptsManager.AppSettingInfo.NameColumn}] ASC ); END; ");
            #endregion
            
            #region Permission Table
            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.PermissionInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{ScriptsManager.PermissionInfo.TableName}] ( ");
            sBuilder.Append($"{ScriptsManager.PermissionInfo.IdColumn} VARCHAR(450) NOT NULL, {ScriptsManager.PermissionInfo.NameColumn} VARCHAR(256) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{ScriptsManager.PermissionInfo.TableName}] PRIMARY KEY ([{ScriptsManager.PermissionInfo.IdColumn}])); ");//END;");
            #endregion
            
            #region RolePermission Table
            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.RolePermissionInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{ScriptsManager.RolePermissionInfo.TableName}] ( ");
            sBuilder.Append($"{ScriptsManager.RolePermissionInfo.IdColumn} INT IDENTITY(1,1) NOT NULL, {ScriptsManager.RolePermissionInfo.RoleIdColumn} VARCHAR(450) NOT NULL, {ScriptsManager.RolePermissionInfo.PermissionIdColumn} VARCHAR(450) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{ScriptsManager.RolePermissionInfo.TableName}] PRIMARY KEY ([{ScriptsManager.RolePermissionInfo.IdColumn}])); ");//END;");
            
            sBuilder.Append($"CREATE NONCLUSTERED INDEX [IX_{ScriptsManager.RolePermissionInfo.TableName}_ON_{ScriptsManager.RolePermissionInfo.RoleIdColumn}] ");
            sBuilder.Append($"ON [dbo].[{ScriptsManager.RolePermissionInfo.TableName}] ");
            sBuilder.Append($"( [{ScriptsManager.RolePermissionInfo.RoleIdColumn}] ASC ); END; ");

            sBuilder.Append($"CREATE UNIQUE NONCLUSTERED INDEX [IX_{ScriptsManager.RolePermissionInfo.TableName}_ON_{ScriptsManager.RolePermissionInfo.RoleIdColumn}_AND_{ScriptsManager.RolePermissionInfo.PermissionIdColumn}] ");
            sBuilder.Append($"ON [dbo].[{ScriptsManager.RolePermissionInfo.TableName}] ");
            sBuilder.Append($"( [{ScriptsManager.RolePermissionInfo.RoleIdColumn}] ASC, [{ScriptsManager.RolePermissionInfo.PermissionIdColumn}] ASC ); END; ");
            #endregion
            
            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.CacheChangeTrackerInfo.UpdateChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{ScriptsManager.CacheChangeTrackerInfo.UpdateChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.CacheChangeTrackerInfo.GetAllChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{ScriptsManager.CacheChangeTrackerInfo.GetAllChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{ScriptsManager.CacheChangeTrackerInfo.GetTableChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{ScriptsManager.CacheChangeTrackerInfo.GetTableChangeTrackerSpName}] ");
            sBuilder.Append("END;");

            getAllChangeTrackerBuilder.Append($"CREATE PROCEDURE [{ScriptsManager.CacheChangeTrackerInfo.GetAllChangeTrackerSpName}] ");
            getAllChangeTrackerBuilder.Append("AS ");
            getAllChangeTrackerBuilder.Append(
                $"SELECT [{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn}], [{ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn}] from [{ScriptsManager.CacheChangeTrackerInfo.TableName}] (NOLOCK)");

            getTableChangeTrackerBuilder.Append(
                $"CREATE PROCEDURE [{ScriptsManager.CacheChangeTrackerInfo.GetTableChangeTrackerSpName}] ({GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)} VARCHAR(255)) ");
            getTableChangeTrackerBuilder.Append("AS ");
            getTableChangeTrackerBuilder.Append(
                $"SELECT [{ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn}] from [{ScriptsManager.CacheChangeTrackerInfo.TableName}] (NOLOCK) WHERE [{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn}] = {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)}");

            updateChangeTrackerBuilder.Append(
                $"CREATE PROCEDURE [{ScriptsManager.CacheChangeTrackerInfo.UpdateChangeTrackerSpName}] ({GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)} VARCHAR(255)) ");
            updateChangeTrackerBuilder.Append("AS ");
            updateChangeTrackerBuilder.Append($"DECLARE {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn)} INT ");
            updateChangeTrackerBuilder.Append(
                $"SELECT {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn)} = [{ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn}] FROM [{ScriptsManager.CacheChangeTrackerInfo.TableName}] (NOLOCK) WHERE [{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn}] = {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)} ");
            updateChangeTrackerBuilder.Append($"IF({GetParameterName(ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn)} IS NULL) " +
                                              $"BEGIN " +
                                              $"INSERT INTO [{ScriptsManager.CacheChangeTrackerInfo.TableName}] ([{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn}], [{ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn}]) VALUES ({GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)}, 1) RETURN 1 " +
                                              $"END ");
            updateChangeTrackerBuilder.Append($"IF({GetParameterName(ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn)} > 1000) ");
            updateChangeTrackerBuilder.Append($"BEGIN " +
                                              $"SET {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn)} = 1 " +
                                              $"END ");
            updateChangeTrackerBuilder.Append(
                $"UPDATE dbo.[{ScriptsManager.CacheChangeTrackerInfo.TableName}] SET [{ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn}] = {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.ChangeIdColumn)} + 1 WHERE [{ScriptsManager.CacheChangeTrackerInfo.TableNameColumn}] = {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)}");

            var dbTriggerCommands = new List<StringBuilder>();
            dbTriggerCommands.AddRange(GetCacheTrackerTriggerCommands(typeof(AppSetting)));
            dbTriggerCommands.AddRange(GetCacheTrackerTriggerCommands(typeof(RolePermission)));
            dbTriggerCommands.AddRange(GetCacheTrackerTriggerCommands(typeof(Permission)));
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

        internal static string GetTableName(Type entityType, bool forQuery = false)
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
                $"BEGIN TRY SET NOCOUNT ON; EXEC [{ScriptsManager.CacheChangeTrackerInfo.UpdateChangeTrackerSpName}] {GetParameterName(ScriptsManager.CacheChangeTrackerInfo.TableNameColumn)} = N'{GetTableName(entityType)}' END TRY BEGIN CATCH END CATCH");

            list.Add(deleteScriptBuilder);
            list.Add(createTriggerScriptBuilder);
            return list;
        }

        public static class CacheChangeTrackerInfo
        {
            public const string TableName = "__SolhigsonCacheChangeTracker";
            public const string TableNameColumn = "TableName";
            public const string ChangeIdColumn = "ChangeId";
            public const string UpdateChangeTrackerSpName = "__SolhigsonUpdateChangeTracker";
            public const string GetAllChangeTrackerSpName = "__SolhigsonGetAllChangeTrackerIds";
            public const string GetTableChangeTrackerSpName = "__SolhigsonGetTableChangeTrackerId";
            
        }

        public static class AppSettingInfo
        {
            public const string TableName = "__SolhigsonAppSettings";
            public const string NameColumn = "Name";
            public const string ValueColumn = "Value";
            public const string IdColumn = "Id";
        }
        
        public static class PermissionInfo
        {
            public const string TableName = "__SolhigsonPermissions";
            public const string NameColumn = "Name";
            public const string IdColumn = "Id";
        }

        public static class RolePermissionInfo
        {
            public const string TableName = "__SolhigsonRolePermissions";
            public const string RoleIdColumn = "RoleId";
            public const string PermissionIdColumn = "PermissionId";
            public const string IdColumn = "Id";
        }
        
    }
}