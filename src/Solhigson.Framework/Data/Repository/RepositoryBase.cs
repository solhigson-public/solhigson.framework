using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

    public RepositoryBase(TDbContext dbContext)
    {
        DbContext = dbContext;
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

    public IQueryable<T> Get(Expression<Func<T, bool>> expression)
    {
        return DbContext.Set<T>().Where(expression);
    }
    
    public IQueryable<TK> Get<TK>(Expression<Func<T, bool>> expression)
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
        
    public async Task<T> AddAndSaveChanges(T entity)
    {
        return await DoAction(DbContext.Set<T>().Add, entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        DbContext.Set<T>().AddRange(entities);
    }
 
    public async Task AddRangeAndSaveChanges(IEnumerable<T> entities)
    {
        await DoAction(DbContext.Set<T>().AddRange, entities);
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
        
    public async Task<T> UpdateAndSaveChanges(T entity)
    {
        return await DoAction(DbContext.Set<T>().Update, entity);
    }
        
    public async Task UpdateRangeAndSaveChanges(IEnumerable<T> entities)
    {
        await DoAction(DbContext.Set<T>().UpdateRange, entities);
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
        
    public async Task<T> RemoveAndSaveChanges(T entity)
    {
        return await DoAction(DbContext.Set<T>().Remove, entity);
    }
        
    public async Task RemoveRangeAndSaveChanges(IEnumerable<T> entities)
    {
        await DoAction(DbContext.Set<T>().RemoveRange, entities);
    }
    #endregion


    private async Task<T> DoAction(Func<T, EntityEntry<T>> method, T entity)
    {
        var ent = method(entity).Entity;
        await DbContext.SaveChangesAsync();
        return ent;
    }
        
    private async Task DoAction(Action<IEnumerable<T>> method, IEnumerable<T> entity)
    {
        method(entity);
        await DbContext.SaveChangesAsync();
    }


}