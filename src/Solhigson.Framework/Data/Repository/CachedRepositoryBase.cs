using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.EfCore;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Data.Repository;

public abstract class CachedRepositoryBase<T, TDbContext, TCacheModel> : RepositoryBase<T, TDbContext>, ICachedRepositoryBase<T, TCacheModel> 
    where T : class, new() where TDbContext : DbContext where TCacheModel : class
{
    public CachedRepositoryBase(TDbContext dbContext) : base(dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<List<TCacheModel>> GetAllCachedAsync()
    {
        return await DbContext.Set<T>().ProjectToType<TCacheModel>().FromCacheListAsync(typeof(T));
    }

    public async Task<List<TCacheModel>> GetListCachedAsync(Expression<Func<T, bool>> expression)
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheListAsync(typeof(T));
    }

    public async Task<TCacheModel?> GetSingleCachedAsync(Expression<Func<T, bool>> expression)
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheSingleAsync(typeof(T));
    }
    
    public async Task<List<TK>> GetListCachedAsync<TK>(Expression<Func<T, bool>> expression) where TK : class
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TK>().FromCacheListAsync(typeof(T));
    }

    public async Task<TK?> GetSingleCachedAsync<TK>(Expression<Func<T, bool>> expression) where TK : class
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TK>().FromCacheSingleAsync(typeof(T));
    }

}