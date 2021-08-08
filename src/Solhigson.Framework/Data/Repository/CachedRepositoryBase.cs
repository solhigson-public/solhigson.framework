using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Data.Repository
{
    public class CachedRepositoryBase<T, TDbContext, TCacheModel> : RepositoryBase<T, TDbContext>, ICachedRepositoryBase<T, TCacheModel> 
        where T : class where TDbContext : DbContext where TCacheModel : class
    {
        public CachedRepositoryBase(TDbContext dbContext) : base(dbContext)
        {
            DbContext = dbContext;
        }

        public IList<TCacheModel> GetListCached(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheList();
        }

        public TCacheModel GetSingleCached(Expression<Func<T, bool>> expression)
        {
            return DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheSingle();
        }
    }
}