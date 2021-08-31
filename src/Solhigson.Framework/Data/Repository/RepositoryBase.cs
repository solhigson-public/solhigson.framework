using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Data.Repository
{
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

        public IQueryable<T> GetAll()
        {
            return DbContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> Get(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression);
        }
        
        public bool Exists(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Any(expression);
        }

        public T Add(T entity)
        {
            return DbContext.Set<T>().Add(entity).Entity;
        }

        public void AddRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().AddRange(entities);
        }

        public T Attach(T entity)
        {
            return DbContext.Set<T>().Attach(entity).Entity;
        }
        
        public void AttachRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().AttachRange(entities);
        }

        public T Update(T entity)
        {
            return DbContext.Set<T>().Update(entity).Entity;
        }
        
        public void UpdateRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().UpdateRange(entities);
        }

        public T Remove(T entity)
        {
            return DbContext.Set<T>().Remove(entity).Entity;
        }
        
        public void RemoveRange(IEnumerable<T> entities)
        {
            DbContext.Set<T>().RemoveRange(entities);
        }

    }
}