using System;
using System.Linq;
using System.Linq.Expressions;

namespace Solhigson.Framework.Data.Repository
{
    public interface IRepositoryBase<T>
    {
        IQueryable<T> GetAll();
        IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression);
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}