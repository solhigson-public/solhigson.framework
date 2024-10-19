using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Common;
using Microsoft.Data.SqlClient;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Data;

public static class AdoNetUtils
{
    private static readonly LogWrapper Logger = new(typeof(AdoNetUtils).FullName);

    public static async Task<int> ExecuteNonQueryAsync(string connectionString, string spNameOrCommand,
        List<SqlParameter> parameters = null,
        bool isStoredProcedure = true,
        SqlRetryLogicBaseProvider retryLogicBaseProvider = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = new())
    {
        await using var conn = new SqlConnection(connectionString);
        if (retryLogicBaseProvider is not null)
        {
            conn.RetryLogicProvider = retryLogicBaseProvider;
        }
        await using var cmd = new SqlCommand(spNameOrCommand, conn);
        if (commandTimeout is not null)
        {
            cmd.CommandTimeout = commandTimeout.Value;
        }
        if (isStoredProcedure) cmd.CommandType = CommandType.StoredProcedure;
        if (parameters is { Count: > 0 }) cmd.Parameters.AddRange(parameters.ToArray());
        await conn.OpenAsync(cancellationToken);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<T> ExecuteSingleOrDefaultAsync<T>(string connectionString, string spNameOrCommand,
        List<SqlParameter> parameters = null, bool isStoredProcedure = true,
        SqlRetryLogicBaseProvider retryLogicBaseProvider = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = new())
    {
        await using var conn = new SqlConnection(connectionString);
        if (retryLogicBaseProvider is not null)
        {
            conn.RetryLogicProvider = retryLogicBaseProvider;
        }
        await using var cmd = new SqlCommand(spNameOrCommand, conn);
        if (commandTimeout is not null)
        {
            cmd.CommandTimeout = commandTimeout.Value;
        }
        await using var reader = await ExecuteReaderAsync(cmd, parameters, isStoredProcedure, cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ReadSingle<T>(reader);
    }
        
    public static async Task<List<T>> ExecuteListAsync<T>(string connectionString, string spNameOrCommand,
        List<SqlParameter> parameters = null, bool isStoredProcedure = true,
        SqlRetryLogicBaseProvider retryLogicBaseProvider = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = new())
    {
        await using var conn = new SqlConnection(connectionString);
        if (retryLogicBaseProvider is not null)
        {
            conn.RetryLogicProvider = retryLogicBaseProvider;
        }
        await using var cmd = new SqlCommand(spNameOrCommand, conn);
        if (commandTimeout is not null)
        {
            cmd.CommandTimeout = commandTimeout.Value;
        }
        await using var reader = await ExecuteReaderAsync(cmd, parameters, isStoredProcedure, cancellationToken);
        return await ReadCollectionAsync<T>(reader, cancellationToken);
    }

        
    private static async Task<SqlDataReader> ExecuteReaderAsync(SqlCommand cmd, List<SqlParameter> parameters = null,
        bool isStoredProcedure = false,
        CancellationToken cancellationToken = new())
    {
        if (cmd == null) throw new Exception("Command cannot be null, in execute reader");
        if (isStoredProcedure) cmd.CommandType = CommandType.StoredProcedure;
        if (parameters is { Count: > 0 }) cmd.Parameters.AddRange(parameters.ToArray());
        await cmd.Connection.OpenAsync(cancellationToken);
        return await cmd.ExecuteReaderAsync(cancellationToken);
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

    private static async Task<List<TK>> ReadCollectionAsync<TK>(DbDataReader reader,
        CancellationToken cancellationToken = new())
    {
        var list = new List<TK>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(ReadSingle<TK>(reader));
        }
        return list;
    }


}