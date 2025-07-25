﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solhigson.Utilities.Extensions;

namespace Solhigson.Framework.Data.Repository;

public abstract class RepositoryBase<T, TDbContext> : IRepositoryBase<T> where T : class, new() where TDbContext : DbContext
{
    protected TDbContext DbContext { get; set; }
    private string ConnectionString { get; }

    public RepositoryBase(TDbContext dbContext)
    {
        DbContext = dbContext;
        var connectionString = dbContext.Database.GetDbConnection().ConnectionString;
        ConnectionString = connectionString;
    }

    public async Task<int> ExecuteNonQueryAsync(string spName, List<SqlParameter>? parameters = null,
        bool isStoredProcedure = true,
        SqlRetryLogicBaseProvider? retryLogicBaseProvider = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await AdoNetUtils.ExecuteNonQueryAsync(ConnectionString,
            spName, parameters, isStoredProcedure, retryLogicBaseProvider, commandTimeout, cancellationToken);
    }
    
    public async Task<TK?> ExecuteSingleOrDefaultAsync<TK>(string spName, List<SqlParameter>? parameters = null,
        bool isStoredProcedure = true,
        SqlRetryLogicBaseProvider? retryLogicBaseProvider = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await AdoNetUtils.ExecuteSingleOrDefaultAsync<TK>(ConnectionString,
            spName, parameters, isStoredProcedure, retryLogicBaseProvider, commandTimeout, cancellationToken);
    }

    public async Task<List<TK>> ExecuteListAsync<TK>(string spName, List<SqlParameter>? parameters = null,
        bool isStoredProcedure = true,
        SqlRetryLogicBaseProvider? retryLogicBaseProvider = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await AdoNetUtils.ExecuteListAsync<TK>(ConnectionString,
            spName, parameters, isStoredProcedure, retryLogicBaseProvider, commandTimeout, cancellationToken);
    }
    
    protected SqlParameter GetParameter(string name, object value)
    {
        return new SqlParameter { ParameterName = name, Value = value };
    }

        
    public T New(object? identifier = null)
    {
        var entity = new T();
        if (identifier != null)
        {
            entity.GetType().GetProperties()
                .FirstOrDefault(t => t.GetAttribute<KeyAttribute>() != null)?.SetValue(entity, identifier);
        }
        return Add(entity);
    }

    [Obsolete("This property is obsolete. Use Where() instead.")]
    public IQueryable<T> Get(Expression<Func<T, bool>> expression)
    {
        return Where(expression);
    }
    
    [Obsolete("This property is obsolete. Use Where<TK>() instead.")]
    public IQueryable<TK> Get<TK>(Expression<Func<T, bool>> expression) where TK : class
    {
        return Where<TK>(expression);
    }
    
    public IQueryable<T> Where(Expression<Func<T, bool>> expression)
    {
        return DbContext.Set<T>().Where(expression);
    }
    
    public IQueryable<TK> Where<TK>(Expression<Func<T, bool>> expression) where TK : class
    {
        return DbContext.Set<T>().Where(expression).AsNoTracking().ProjectToType<TK>();
    }


    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().AnyAsync(expression, cancellationToken: cancellationToken);
    }

    #region Add
    public T Add(T entity)
    {
        return DbContext.Add(entity).Entity;
    }
        
    public async Task<T> AddAndSaveChangesAsync(T entity, CancellationToken cancellationToken = default)
    {
        return await DoActionAndSaveChangesAsync(DbContext.Set<T>().Add, entity, cancellationToken);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        DbContext.AddRange(entities);
    }
 
    public async Task AddRangeAndSaveChangesAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DoActionAndSaveChangesAsync(DbContext.Set<T>().AddRange, entities, cancellationToken);
    }
    #endregion

    public T Attach(T entity)
    {
        return DbContext.Attach(entity).Entity;
    }
        
    public void AttachRange(IEnumerable<T> entities)
    {
        DbContext.AttachRange(entities);
    }

    #region Update
    public T Update(T entity)
    {
        return DbContext.Set<T>().Update(entity).Entity;
    }
        
    public void UpdateRange(IEnumerable<T> entities)
    {
        DbContext.Set<T>().UpdateRange(entities);
    }
        
    public async Task<T> UpdateAndSaveChangesAsync(T entity, CancellationToken cancellationToken = default)
    {
        return await DoActionAndSaveChangesAsync(DbContext.Set<T>().Update, entity, cancellationToken);
    }
        
    public async Task UpdateRangeAndSaveChangesAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DoActionAndSaveChangesAsync(DbContext.Set<T>().UpdateRange, entities, cancellationToken);
    }
    #endregion

    #region Remove
    public T Remove(T entity)
    {
        return DbContext.Set<T>().Remove(entity).Entity;
    }
        
    public void RemoveRange(IEnumerable<T> entities)
    {
        DbContext.Set<T>().RemoveRange(entities);
    }
        
    public async Task<T> RemoveAndSaveChangesAsync(T entity, CancellationToken cancellationToken = default)
    {
        return await DoActionAndSaveChangesAsync(DbContext.Set<T>().Remove, entity, cancellationToken);
    }
        
    public async Task RemoveRangeAndSaveChangesAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DoActionAndSaveChangesAsync(DbContext.Set<T>().RemoveRange, entities, cancellationToken);
    }
    #endregion


    private async Task<T> DoActionAndSaveChangesAsync(Func<T, EntityEntry<T>> method, T entity, CancellationToken cancellationToken = default)
    {
        var ent = method(entity).Entity;
        await DbContext.SaveChangesAsync(cancellationToken);
        return ent;
    }
        
    private async Task DoActionAndSaveChangesAsync(Action<IEnumerable<T>> method, IEnumerable<T> entity, CancellationToken cancellationToken = default)
    {
        method(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }


}