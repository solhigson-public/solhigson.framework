﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
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

    public async Task<List<TCacheModel>> GetAllCachedAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().ProjectToType<TCacheModel>().FromCacheListAsync(cancellationToken, typeof(T));
    }

    public async Task<List<TCacheModel>> GetListCachedAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheListAsync(cancellationToken, typeof(T));
    }

    public async Task<TCacheModel?> GetSingleCachedAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheSingleAsync(cancellationToken, typeof(T));
    }
    
    public async Task<List<TK>> GetListCachedAsync<TK>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default) where TK : class
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TK>().FromCacheListAsync(cancellationToken, typeof(T));
    }

    public async Task<TK?> GetSingleCachedAsync<TK>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default) where TK : class
    {
        return await DbContext.Set<T>().Where(expression).ProjectToType<TK>().FromCacheSingleAsync(cancellationToken, typeof(T));
    }

}