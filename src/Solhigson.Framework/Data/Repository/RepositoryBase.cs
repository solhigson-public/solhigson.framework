using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Data.Repository;

public abstract class RepositoryBase<T, TDbContext> : IRepositoryBase<T> where T : class, new() where TDbContext : DbContext
{
    protected TDbContext DbContext { get; set; }
    private string ConnectionString { get; }

    public RepositoryBase(TDbContext dbContext)
    {
        DbContext = dbContext;
        ConnectionString = DbContext.Database.GetConnectionString();
    }

    public async Task<int> ExecuteNonQueryAsync(string spName, List<SqlParameter> parameters = null,
        bool isStoredProcedure = true)
    {
        return await AdoNetUtils.ExecuteNonQueryAsync(ConnectionString,
            spName, parameters, isStoredProcedure);
    }
    
    public async Task<TK> ExecuteSingleOrDefaultAsync<TK>(string spName, List<SqlParameter> parameters = null,
        bool isStoredProcedure = true)
    {
        return await AdoNetUtils.ExecuteSingleOrDefaultAsync<TK>(ConnectionString,
            spName, parameters, isStoredProcedure);
    }

    public async Task<List<TK>> ExecuteListAsync<TK>(string spName, List<SqlParameter> parameters = null,
        bool isStoredProcedure = true)
    {
        return await AdoNetUtils.ExecuteListAsync<TK>(ConnectionString,
            spName, parameters, isStoredProcedure);
    }
    
    protected SqlParameter GetParameter(string name, object value)
    {
        return new SqlParameter { ParameterName = name, Value = value };
    }

        
    public T New(object identifier = null)
    {
        var entity = new T();
        if (identifier != null)
        {
            entity.GetType().GetProperties()
                .FirstOrDefault(t => t.GetAttribute<KeyAttribute>() != null)?.SetValue(entity, identifier);
        }
        return Add(entity);
    }

    [ObsoleteAttribute("This property is obsolete. Use Where() instead.")]
    public IQueryable<T> Get(Expression<Func<T, bool>> expression)
    {
        return Where(expression);
    }
    
    [ObsoleteAttribute("This property is obsolete. Use Where<TK>() instead.")]
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


    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> expression)
    {
        return await DbContext.Set<T>().AnyAsync(expression);
    }

    #region Add
    public T Add(T entity)
    {
        return DbContext.Set<T>().Add(entity).Entity;
    }
        
    public async Task<T> AddAndSaveChangesAsync(T entity)
    {
        return await DoActionAndSaveChangesAsync(DbContext.Set<T>().Add, entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        DbContext.Set<T>().AddRange(entities);
    }
 
    public async Task AddRangeAndSaveChangesAsync(IEnumerable<T> entities)
    {
        await DoActionAndSaveChangesAsync(DbContext.Set<T>().AddRange, entities);
    }
    #endregion

    public T Attach(T entity)
    {
        return DbContext.Set<T>().Attach(entity).Entity;
    }
        
    public void AttachRange(IEnumerable<T> entities)
    {
        DbContext.Set<T>().AttachRange(entities);
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
        
    public async Task<T> UpdateAndSaveChangesAsync(T entity)
    {
        return await DoActionAndSaveChangesAsync(DbContext.Set<T>().Update, entity);
    }
        
    public async Task UpdateRangeAndSaveChangesAsync(IEnumerable<T> entities)
    {
        await DoActionAndSaveChangesAsync(DbContext.Set<T>().UpdateRange, entities);
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
        
    public async Task<T> RemoveAndSaveChangesAsync(T entity)
    {
        return await DoActionAndSaveChangesAsync(DbContext.Set<T>().Remove, entity);
    }
        
    public async Task RemoveRangeAndSaveChangesAsync(IEnumerable<T> entities)
    {
        await DoActionAndSaveChangesAsync(DbContext.Set<T>().RemoveRange, entities);
    }
    #endregion


    private async Task<T> DoActionAndSaveChangesAsync(Func<T, EntityEntry<T>> method, T entity)
    {
        var ent = method(entity).Entity;
        await DbContext.SaveChangesAsync();
        return ent;
    }
        
    private async Task DoActionAndSaveChangesAsync(Action<IEnumerable<T>> method, IEnumerable<T> entity)
    {
        method(entity);
        await DbContext.SaveChangesAsync();
    }


}