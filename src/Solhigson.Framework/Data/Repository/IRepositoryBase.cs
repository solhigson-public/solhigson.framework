using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository
{
    public interface IRepositoryBase<T> where T : class, new()
    {
        T New();
        IQueryable<T> GetAll();
        IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression);
        bool ExistsWithCondition(Expression<Func<T, bool>> expression);
        EntityEntry<T> Add(T entity);
        void AddRange(IEnumerable<T> entities);
        EntityEntry<T> Attach(T entity);
        void AttachRange(IEnumerable<T> entities);
        EntityEntry<T> Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        EntityEntry<T> Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}