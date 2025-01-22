using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;

namespace Solhigson.Framework.EfCore.Caching;

public interface ICacheProvider
{
    Task<bool> InvalidateCacheAsync(Type[] types);

    Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types) where T : class;

    Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey) where T : class;
}