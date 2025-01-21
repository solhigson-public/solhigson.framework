using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Dto;
using Solhigson.Framework.EfCore.Caching;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging;
using Solhigson.Utilities.Extensions;
using Solhigson.Utilities.Linq;
using StackExchange.Redis;

namespace Solhigson.Framework.EfCore;

public static class Extensions
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(Extensions).FullName);
    #region EntityFramework Data Extensions (Caching & Paging)

    public static void ResyncCache<T>(this T? dbContext) where T : DbContext
    {
        if (dbContext is null)
        {
            return;
        }
        List<Type> types = [];
        var cachedEntityType = typeof(ICachedEntity);
        var props = dbContext.GetType().GetProperties();
        
        foreach (var prop in props.Where(t => t.PropertyType.IsDbSetType()))
        {
            var genericArg = prop.PropertyType.GetGenericArguments().FirstOrDefault();
            if (genericArg is not null && cachedEntityType.IsAssignableFrom(genericArg))
            {
                types.Add(genericArg);
            }
        }
        
        if (types.HasData())
        {
            _ = EfCoreCacheManager.InvalidateAsync(types.ToArray());
        }
    }
    
    public static IApplicationBuilder InitializeEfCoreRedisCache(this IApplicationBuilder app, 
        IConnectionMultiplexer? connectionMultiplexer,
        string? prefix = null, int expirationInMinutes = 1440)
    {
        EfCoreCacheManager.Initialize(CacheType.Redis, connectionMultiplexer, prefix, expirationInMinutes);
        return app;
    }
    
    public static IApplicationBuilder InitializeEfCoreMemoryCache(this IApplicationBuilder app, 
        IConnectionMultiplexer? connectionMultiplexer,
        string? prefix = null, int expirationInMinutes = 1440, int changeTrackerTimerIntervalInSeconds = 5)
    {
        EfCoreCacheManager.Initialize(CacheType.Memory, connectionMultiplexer, prefix, expirationInMinutes, 
            changeTrackerTimerIntervalInSeconds);
        return app;
    }

    
    public static string GetCacheKey<T>(this IQueryable<T> query, bool hash = true) where T : class
    {
        var expression = query.Expression;

        // locally evaluate as much of the query as possible
        expression = Evaluator.PartialEval(expression, Evaluator.CanBeEvaluatedLocallyFunc);

        // support local collections
        expression = LocalCollectionExpander.Rewrite(expression);

        // use the string representation of the expression for the cache key
        var key = $"{GetQueryBaseTypeSingle(query)}{expression}";

        if (hash)
        {
            key = key.Sha512();
        }

        return key;
    }

    public static ResponseInfo<object> GetCacheStatus<T>(this IQueryable<T> query, params Type[]? iCachedEntityType)
        where T : class
    {
        var response = new ResponseInfo<object>();
        var validTypes = GetQueryBaseTypeList(query, iCachedEntityType);
        validTypes ??= Array.Empty<Type>();
        var types = new List<string?>();
        if (iCachedEntityType?.Length > 0)
        {
            types.AddRange(iCachedEntityType.Select(t => t.FullName));
        }
        else
        {
            types.Add(GetQueryBaseTypeSingle(query).FullName);
        }

        var queryExpression = query.GetCacheKey(false);
        var cacheInfo = new Dictionary<string, List<string?>>
        {
            { "QueryTypes", types },
            { "ValidCacheTypes", validTypes.Select(t => t.FullName).ToList() }
        };
        var data = new
        {
            CacheKey = queryExpression.Sha512(),
            QueryExpression = queryExpression,
            TypesInfo = cacheInfo
        };
        if (validTypes.Length == types.Count)
        {
            return response.Success(data);
        }

        return !validTypes.Any()
            ? response.Fail("No Caching", result: data)
            : response.Fail("Partial Caching", "90001", result: data);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="iCachedEntityTypesToMonitor">The entity types to monitor for database changes</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<List<T>> FromCacheListAsync<T>(this IQueryable<T> query, params Type[] iCachedEntityTypesToMonitor)
        where T : class
    {
        return await GetCacheDataAsync(query, ResolveToList, iCachedEntityTypesToMonitor) ?? new List<T>();
    }

    public static async Task<PagedList<T>> FromCacheListPagedAsync<T>(this IQueryable<T> query, int page, int itemsPerPage,
        params Type[] iCachedEntityTypesToMonitor) where T : class
    {
        var data = await GetCacheDataAsync(query, ResolveToList, iCachedEntityTypesToMonitor) ?? [];
        return data.AsQueryable().ToPagedList(page, itemsPerPage);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="iCachedEntityTypesToMonitor">The entity types to monitor for database changes</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T?> FromCacheSingleAsync<T>(this IQueryable<T> query, params Type[] iCachedEntityTypesToMonitor)
        where T : class
    {
        return await GetCacheDataAsync(query, ResolveToSingle, iCachedEntityTypesToMonitor);
    }

    public static async Task<bool> AddCustomResultToCacheAsync<T>(this IQueryable<T> query, object result, params Type[] types)
        where T : class
    {
        return await EfCoreCacheManager.SetDataAsync(query.GetCacheKey(), result, types);
    }

    public static async Task<T?> GetCustomResultFromCacheAsync<T, TK>(this IQueryable<TK> query) where T : class where TK : class
    {
        var result = await EfCoreCacheManager.GetDataAsync<T>(query.GetCacheKey());
        if (!result.IsSuccessful)
        {
            return null;
        }

        Logger.LogTrace($"Retrieved {query.ElementType.Name} [{query.GetCacheKey(false)}] data from cache");
        return result.Data;
    }


    private static async Task<TK?> GetCacheDataAsync<T, TK>(IQueryable<T> query, Func<IQueryable<T>, Task<TK?>> func,
        params Type[] iCachedEntityTypes)
        where TK : class where T : class
    {
        var validTypes = GetQueryBaseTypeList(query, iCachedEntityTypes);
        if (!validTypes.HasData())
        {
            return await func(query);
        }
        var key = query.GetCacheKey();
        var entryResult = await EfCoreCacheManager.GetDataAsync<TK>(key);
        if (entryResult.IsSuccessful)
        {
            Logger.LogTrace($"Retrieved {query.ElementType.Name} [{query.GetCacheKey(false)}] data from cache");
            return entryResult.Data;
        }

        lock (key)
        {
            entryResult = EfCoreCacheManager.GetDataAsync<TK>(key).Result;
            if (entryResult.IsSuccessful)
            {
                return entryResult.Data;
            }

            Logger.LogTrace($"Fetching {query.ElementType.Name} [{query.GetCacheKey(false)}] data from db");
            var result = func(query).Result;// as TK;

            _ = EfCoreCacheManager.SetDataAsync(key, result, validTypes);

            return result;
        }
    }

    private static Type GetQueryBaseTypeSingle<T>(IQueryable<T> query) where T : class
    {
        var type = typeof(T);
        try
        {
            if (query.Expression is System.Linq.Expressions.MethodCallExpression me)
            {
                if (me.Arguments.Count > 0 && me.Arguments[0].Type.GenericTypeArguments.Length > 0)
                {
                    type = me.Arguments[0].Type.GenericTypeArguments[0];
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        return type;
    }

    // private static bool IsValidICacheEntityTypes<T>(IQueryable<T> query, params Type[]? iCachedEntityTypes)
    //     where T : class
    // {
    //     if (iCachedEntityTypes != null && iCachedEntityTypes.Length != 0)
    //     {
    //         return IsValidICacheEntityTypes(iCachedEntityTypes);
    //     }
    //
    //     return IsValidICacheEntityTypes(GetQueryBaseTypeSingle(query));
    // }

    private static async Task<List<T>?> ResolveToList<T>(IQueryable<T> query) where T : class
    {
        var result = await query.AsNoTrackingWithIdentityResolution().ToListAsync();
        return result.Count != 0 ? result : null;
    }

    private static async Task<T?> ResolveToSingle<T>(IQueryable<T> query) where T : class
    {
        return await query.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync();
    }

    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageNumber,
        int itemsPerPage) where T : class
    {
        var count = await source.CountAsync();
        var items = await source.AsNoTrackingWithIdentityResolution().Skip((pageNumber - 1) * itemsPerPage)
            .Take(itemsPerPage).ToListAsync();
        return PagedList.Create(items, count, pageNumber, itemsPerPage);
    }

    public static PagedList<T> ToPagedList<T>(this IQueryable<T> source, int pageNumber, int itemsPerPage)
        where T : class
    {
        var count = source.Count();
        var items = source.AsNoTrackingWithIdentityResolution().Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage)
            .ToList();
        return PagedList.Create(items, count, pageNumber, itemsPerPage);
    }
    
    // private static bool IsValidICacheEntityTypes(params Type []? types)
    // {
    //     return types?.Any(type => typeof(ICachedEntity).IsAssignableFrom(type)) == true;
    // }

    private static Type[]? GetQueryBaseTypeList<T>(IQueryable<T> query, params Type[]? iCachedEntityTypes)
        where T : class
    {
        return iCachedEntityTypes.HasData() ? iCachedEntityTypes : [GetQueryBaseTypeSingle(query)];
    }

    private static List<Type>? GetQueryBaseTypeList(params Type []? types)
    {
        return types?.Where(type => typeof(ICachedEntity).IsAssignableFrom(type)).ToList();
    }


    #endregion
}