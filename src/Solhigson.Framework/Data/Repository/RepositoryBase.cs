﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository
{
    public class RepositoryBase<T, TK> : IRepositoryBase<T> where T : class where TK : DbContext
    {
        protected TK DbContext { get; }

        public RepositoryBase(TK dbContext)
        {
            DbContext = dbContext;
        }

        public IQueryable<T> GetAll()
        {
            return DbContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression);
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