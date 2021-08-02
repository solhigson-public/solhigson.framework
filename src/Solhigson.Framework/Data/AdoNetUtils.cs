using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Data
{
    public static class AdoNetUtils
    {
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();

        public static async Task<int> ExecuteNonQueryAsync(string connectionString, string spNameOrCommand,
            List<SqlParameter> parameters = null,
            bool isStoredProcedure = false)
        {
            await using var conn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(spNameOrCommand, conn);
            if (isStoredProcedure) cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Count > 0) cmd.Parameters.AddRange(parameters.ToArray());
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<T> GetSingleOrDefaultAsync<T>(string connectionString, string spNameOrCommand,
            List<SqlParameter> parameters = null, bool isStoredProcedure = false)
        {
            await using var conn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(spNameOrCommand, conn);
            await using var reader = await ExecuteReaderAsync(cmd, parameters, isStoredProcedure);
            await reader.ReadAsync();
            return ReadSingle<T>(reader);
        }
        
        public static async Task<List<T>> GetListAsync<T>(string connectionString, string spNameOrCommand,
            List<SqlParameter> parameters = null, bool isStoredProcedure = false)
        {
            await using var conn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(spNameOrCommand, conn);
            await using var reader = await ExecuteReaderAsync(cmd, parameters, isStoredProcedure);
            return await ReadCollectionAsync<T>(reader);
        }

        
        private static async Task<SqlDataReader> ExecuteReaderAsync(SqlCommand cmd, List<SqlParameter> parameters = null,
            bool isStoredProcedure = false)
        {
            if (cmd == null) throw new Exception("Command cannot be null, in execute reader");
            if (isStoredProcedure) cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Count > 0) cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.Connection.OpenAsync();
            return await cmd.ExecuteReaderAsync();
        }


        private static T SafeReturnValue<T>(object value)
        {
            if (value == null || value is DBNull) return default;
            var returnType = typeof(T);
            var dataType = value.GetType();
            if (returnType != dataType)
            {
                try
                {
                    return (T) Convert.ChangeType(value, returnType);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            return (T) value;
        }

        private static T ReadSingle<T>(DbDataReader reader)
        {
            if (!reader.HasRows) return default;
            var type = typeof(T);
            if (TypeCanBeInstantiated(type))
            {
                //    _logger.Debug("Type is class...");
                var obj = Activator.CreateInstance<T>();
                var objectType = obj.GetType();
                var objProperties = objectType.GetProperties();
                foreach(var pInfo in objProperties)
                {
                    try
                    {
                        var fieldName = pInfo.Name;
                        var value = reader.GetValue(fieldName);
                        if (value is DBNull) continue;

                        var vType = value.GetType();
                        if (vType != pInfo.PropertyType)
                        {
                            var nullableUnderlyingType = Nullable.GetUnderlyingType(pInfo.PropertyType);
                            if (nullableUnderlyingType == null)
                                value = Convert.ChangeType(value, pInfo.PropertyType);
                            else if (vType != nullableUnderlyingType)
                                value = Convert.ChangeType(value, nullableUnderlyingType);
                        }

                        pInfo.SetValue(obj, value, null);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"While reading property values for class: {type.FullName}");
                    }
                }

                return obj;
            }

            var data = reader[0];
            return data is DBNull ? default : SafeReturnValue<T>(data);
        }
        
        private static bool TypeCanBeInstantiated(Type type)
        {
            return !type.IsPrimitive && type != typeof(string) && type != typeof(decimal) && !type.IsArray;
        }

        private static async Task<List<TK>> ReadCollectionAsync<TK>(DbDataReader reader)
        {
            var list = new List<TK>();
            while (await reader.ReadAsync()) list.Add(ReadSingle<TK>(reader));
            return list;
        }


    }
}