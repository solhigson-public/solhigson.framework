using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Solhigson.Framework.Data.Repository
{
    public class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected DbContext DbContext { get; set; }

        public RepositoryBase(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        public IQueryable<T> FindAll()
        {
            return DbContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression).AsNoTracking();
        }

        public void Create(T entity)
        {
            DbContext.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            DbContext.Set<T>().Update(entity);
        }

        public void Delete(T entity)
        {
            DbContext.Set<T>().Remove(entity);
        }
    }
}