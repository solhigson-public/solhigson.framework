using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository
{
    public abstract class RepositoryBase<T, TDbContext> : IRepositoryBase<T> where T : class, new() where TDbContext : DbContext
    {
        protected TDbContext DbContext { get; set; }

        public RepositoryBase(TDbContext dbContext)
        {
            DbContext = dbContext;
        }
        
        public T New()
        {
            var entity = new T();
            return Add(entity).Entity;
        }

        public IQueryable<T> GetAll()
        {
            return DbContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression);
        }
        
        public bool ExistsWithCondition(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Any(expression);
        }


        public EntityEntry<T> Add(T entity)
        {
            return DbContext.Set<T>().Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().AddRange(entities);
        }

        public EntityEntry<T> Attach(T entity)
        {
            return DbContext.Set<T>().Attach(entity);
        }
        
        public void AttachRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().AttachRange(entities);
        }

        public EntityEntry<T> Update(T entity)
        {
            return DbContext.Set<T>().Update(entity);
        }
        
        public void UpdateRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().UpdateRange(entities);
        }

        public EntityEntry<T> Remove(T entity)
        {
            return DbContext.Set<T>().Remove(entity);
        }
        
        public void RemoveRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().RemoveRange(entities);
        }

    }
}