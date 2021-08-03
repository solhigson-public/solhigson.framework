using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Data.Repository
{
    public class CachedRepositoryBase<T, TK> : RepositoryBase<T, TK> where T : class, ICachedEntity where TK : DbContext
    {
        public CachedRepositoryBase(TK dbContext) : base(dbContext)
        {
            
        }

        public IList<T> GetAllCached()
        {
            return DbContext.Set<T>().FromCacheCollection();
        }

        public T GetByConditionCached(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression).FromCacheSingle();
        }
    }
}