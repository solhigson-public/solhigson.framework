using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Solhigson.Framework.Dto;

namespace Solhigson.Framework.EfCore.Caching;

public interface ICacheProvider
{
    Task<bool> InvalidateCacheAsync(Type[] types, CancellationToken cancellationToken = default);

    Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types, CancellationToken cancellationToken = default) where T : class;

    Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class;
}