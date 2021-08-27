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
        internal static void SetUpDatabaseObjects(Assembly databaseModelsAssembly, string connectionString)
        {
            var sBuilder = new StringBuilder();
            var getAllChangeTrackerBuilder = new StringBuilder();
            var getTableChangeTrackerBuilder = new StringBuilder();
            var updateChangeTrackerBuilder = new StringBuilder();

            //Clean up cache monitor table and all triggers, for entities that might have been removed as ICacheEntity
            var cleanUpScript = $@"DECLARE @sql NVARCHAR(MAX) = N'Delete from [{CacheChangeTrackerInfo.TableName}];';
                SELECT @sql += 
                    N'DROP TRIGGER ' + 
                    QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + 
                    QUOTENAME(t.name) + N'; ' + NCHAR(13)
                FROM sys.triggers AS t
                WHERE t.is_ms_shipped = 0
                  AND t.parent_class_desc = N'OBJECT_OR_COLUMN';

                exec (N'' + @sql + N'');
                ";

            sBuilder.Append($"IF OBJECT_ID(N'[{CacheChangeTrackerInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{CacheChangeTrackerInfo.TableName}] ( ");
            sBuilder.Append($"{CacheChangeTrackerInfo.TableNameColumn} VARCHAR(255) NOT NULL, {CacheChangeTrackerInfo.ChangeIdColumn} SMALLINT NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{CacheChangeTrackerInfo.TableName}] PRIMARY KEY ([{CacheChangeTrackerInfo.TableNameColumn}])); END;");

            #region AppSettings Table
            sBuilder.Append($"IF OBJECT_ID(N'[{AppSettingInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{AppSettingInfo.TableName}] ( ");
            sBuilder.Append($"{AppSettingInfo.IdColumn} INT IDENTITY(1,1) NOT NULL, {AppSettingInfo.NameColumn} VARCHAR(255) NOT NULL, {AppSettingInfo.ValueColumn} VARCHAR(MAX) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{AppSettingInfo.TableName}] PRIMARY KEY ([{AppSettingInfo.IdColumn}])); ");//END;");

            sBuilder.Append($"CREATE UNIQUE NONCLUSTERED INDEX [UIX_{AppSettingInfo.TableName}_ON_{AppSettingInfo.NameColumn}] ");
            sBuilder.Append($"ON [dbo].[{AppSettingInfo.TableName}] ");
            sBuilder.Append($"( [{AppSettingInfo.NameColumn}] ASC ); END; ");
            #endregion
            
            #region Permission Table
            sBuilder.Append($"IF OBJECT_ID(N'[{PermissionInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{PermissionInfo.TableName}] ( ");
            sBuilder.Append($"{PermissionInfo.IdColumn} VARCHAR(450) NOT NULL, {PermissionInfo.NameColumn} VARCHAR(256) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{PermissionInfo.TableName}] PRIMARY KEY ([{PermissionInfo.IdColumn}])); ");//END;");
            #endregion
            
            #region RolePermission Table
            sBuilder.Append($"IF OBJECT_ID(N'[{RolePermissionInfo.TableName}]') IS NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"CREATE TABLE [{RolePermissionInfo.TableName}] ( ");
            sBuilder.Append($"{RolePermissionInfo.IdColumn} INT IDENTITY(1,1) NOT NULL, {RolePermissionInfo.RoleIdColumn} VARCHAR(450) NOT NULL, {RolePermissionInfo.PermissionIdColumn} VARCHAR(450) NOT NULL ");
            sBuilder.Append($"CONSTRAINT [PK__{RolePermissionInfo.TableName}] PRIMARY KEY ([{RolePermissionInfo.IdColumn}])); ");//END;");
            
            sBuilder.Append($"CREATE NONCLUSTERED INDEX [IX_{RolePermissionInfo.TableName}_ON_{RolePermissionInfo.RoleIdColumn}] ");
            sBuilder.Append($"ON [dbo].[{RolePermissionInfo.TableName}] ");
            sBuilder.Append($"( [{RolePermissionInfo.RoleIdColumn}] ASC ); END; ");

            sBuilder.Append($"CREATE UNIQUE NONCLUSTERED INDEX [IX_{RolePermissionInfo.TableName}_ON_{RolePermissionInfo.RoleIdColumn}_AND_{RolePermissionInfo.PermissionIdColumn}] ");
            sBuilder.Append($"ON [dbo].[{RolePermissionInfo.TableName}] ");
            sBuilder.Append($"( [{RolePermissionInfo.RoleIdColumn}] ASC, [{RolePermissionInfo.PermissionIdColumn}] ASC ); END; ");
            #endregion
            
            sBuilder.Append($"IF OBJECT_ID(N'[{CacheChangeTrackerInfo.UpdateChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{CacheChangeTrackerInfo.UpdateChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{CacheChangeTrackerInfo.GetAllChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{CacheChangeTrackerInfo.GetAllChangeTrackerSpName}] ");
            sBuilder.Append("END;");
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append($"IF OBJECT_ID(N'[{CacheChangeTrackerInfo.GetTableChangeTrackerSpName}]') IS NOT NULL ");
            sBuilder.Append("BEGIN ");
            sBuilder.Append($"DROP PROCEDURE [{CacheChangeTrackerInfo.GetTableChangeTrackerSpName}] ");
            sBuilder.Append("END;");

            getAllChangeTrackerBuilder.Append($"CREATE PROCEDURE [{CacheChangeTrackerInfo.GetAllChangeTrackerSpName}] ");
            getAllChangeTrackerBuilder.Append("AS ");
            getAllChangeTrackerBuilder.Append(
                $"SELECT [{CacheChangeTrackerInfo.TableNameColumn}], [{CacheChangeTrackerInfo.ChangeIdColumn}] from [{CacheChangeTrackerInfo.TableName}] (NOLOCK)");

            getTableChangeTrackerBuilder.Append(
                $"CREATE PROCEDURE [{CacheChangeTrackerInfo.GetTableChangeTrackerSpName}] ({GetParameterName(CacheChangeTrackerInfo.TableNameColumn)} VARCHAR(255)) ");
            getTableChangeTrackerBuilder.Append("AS ");
            getTableChangeTrackerBuilder.Append(
                $"SELECT [{CacheChangeTrackerInfo.ChangeIdColumn}] from [{CacheChangeTrackerInfo.TableName}] (NOLOCK) WHERE [{CacheChangeTrackerInfo.TableNameColumn}] = {GetParameterName(CacheChangeTrackerInfo.TableNameColumn)}");

            updateChangeTrackerBuilder.Append(
                $"CREATE PROCEDURE [{CacheChangeTrackerInfo.UpdateChangeTrackerSpName}] ({GetParameterName(CacheChangeTrackerInfo.TableNameColumn)} VARCHAR(255)) ");
            updateChangeTrackerBuilder.Append("AS ");
            updateChangeTrackerBuilder.Append($"DECLARE {GetParameterName(CacheChangeTrackerInfo.ChangeIdColumn)} INT ");
            updateChangeTrackerBuilder.Append(
                $"SELECT {GetParameterName(CacheChangeTrackerInfo.ChangeIdColumn)} = [{CacheChangeTrackerInfo.ChangeIdColumn}] FROM [{CacheChangeTrackerInfo.TableName}] (NOLOCK) WHERE [{CacheChangeTrackerInfo.TableNameColumn}] = {GetParameterName(CacheChangeTrackerInfo.TableNameColumn)} ");
            updateChangeTrackerBuilder.Append($"IF({GetParameterName(CacheChangeTrackerInfo.ChangeIdColumn)} IS NULL) " +
                                              $"BEGIN " +
                                              $"INSERT INTO [{CacheChangeTrackerInfo.TableName}] ([{CacheChangeTrackerInfo.TableNameColumn}], [{CacheChangeTrackerInfo.ChangeIdColumn}]) VALUES ({GetParameterName(CacheChangeTrackerInfo.TableNameColumn)}, 1) RETURN 1 " +
                                              $"END ");
            updateChangeTrackerBuilder.Append($"IF({GetParameterName(CacheChangeTrackerInfo.ChangeIdColumn)} > 1000) ");
            updateChangeTrackerBuilder.Append($"BEGIN " +
                                              $"SET {GetParameterName(CacheChangeTrackerInfo.ChangeIdColumn)} = 1 " +
                                              $"END ");
            updateChangeTrackerBuilder.Append(
                $"UPDATE dbo.[{CacheChangeTrackerInfo.TableName}] SET [{CacheChangeTrackerInfo.ChangeIdColumn}] = {GetParameterName(CacheChangeTrackerInfo.ChangeIdColumn)} + 1 WHERE [{CacheChangeTrackerInfo.TableNameColumn}] = {GetParameterName(CacheChangeTrackerInfo.TableNameColumn)}");

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

            using (var conn = new SqlConnection(connectionString))
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
                $"BEGIN TRY SET NOCOUNT ON; EXEC [{CacheChangeTrackerInfo.UpdateChangeTrackerSpName}] {GetParameterName(CacheChangeTrackerInfo.TableNameColumn)} = N'{GetTableName(entityType)}' END TRY BEGIN CATCH END CATCH");

            list.Add(deleteScriptBuilder);
            list.Add(createTriggerScriptBuilder);
            return list;
        }
        
        internal static string GetParameterName(string tableName)
        {
            return $"@{tableName}";
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