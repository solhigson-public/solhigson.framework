using System;
using System.Collections.Generic;
using System.Data;
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

        public static async Task<T> ExecuteScalarAsync<T>(string connectionString, string spNameOrCommand,
            List<SqlParameter> parameters = null, bool isStoredProcedure = true)
        {
            await using var conn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(spNameOrCommand, conn);
            if (isStoredProcedure) cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Count > 0) cmd.Parameters.AddRange(parameters.ToArray());
            await conn.OpenAsync();
            var val = await cmd.ExecuteScalarAsync();
            return SafeReturnValue<T>(val);
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
    }
}