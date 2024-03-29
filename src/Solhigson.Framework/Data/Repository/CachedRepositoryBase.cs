﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Data.Repository;

public abstract class CachedRepositoryBase<T, TDbContext, TCacheModel> : RepositoryBase<T, TDbContext>, ICachedRepositoryBase<T, TCacheModel> 
    where T : class, new() where TDbContext : DbContext where TCacheModel : class
{
    public CachedRepositoryBase(TDbContext dbContext) : base(dbContext)
    {
        DbContext = dbContext;
    }

    public List<TCacheModel> GetAllCached()
    {
        return DbContext.Set<T>().ProjectToType<TCacheModel>().FromCacheList(typeof(T));
    }

    public List<TCacheModel> GetListCached(Expression<Func<T, bool>> expression)
    {
        return DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheList(typeof(T));
    }

    public TCacheModel GetSingleCached(Expression<Func<T, bool>> expression)
    {
        return DbContext.Set<T>().Where(expression).ProjectToType<TCacheModel>().FromCacheSingle(typeof(T));
    }
    
    public List<TK> GetListCached<TK>(Expression<Func<T, bool>> expression) where TK : class
    {
        return DbContext.Set<T>().Where(expression).ProjectToType<TK>().FromCacheList(typeof(T));
    }

    public TK GetSingleCached<TK>(Expression<Func<T, bool>> expression) where TK : class
    {
        return DbContext.Set<T>().Where(expression).ProjectToType<TK>().FromCacheSingle(typeof(T));
    }

}