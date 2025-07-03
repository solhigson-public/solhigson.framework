// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Solhigson.Framework.Dto;
// using Solhigson.Utilities;
// using StackExchange.Redis;
//
// namespace Solhigson.Framework.EfCore.Caching;
//
// public class RedisCacheProvider : CacheProviderBase
// {
//     internal RedisCacheProvider(IConnectionMultiplexer redis, string prefix, int expirationInMinutes = 1440) : 
//         base(redis, prefix, expirationInMinutes)
//     {
//     }
//     
//     public override async Task<bool> InvalidateCacheAsync(Type[] types, CancellationToken cancellationToken = default)
//     {
//         List<string> cacheKeys = [];
//         var tran = Database.CreateTransaction();
//         
//         foreach (var type in types)
//         {
//             var tagCacheKey = GetTagKey(type);
//             var values = await Database.SetMembersAsync(tagCacheKey);
//             if (values.Length != 0)
//             {
//                 cacheKeys.AddRange(values.Select(value => value.ToString()));
//             }
//             _ = tran.KeyDeleteAsync(tagCacheKey);
//         }
//         foreach (var cacheKey in cacheKeys)
//         {
//             _ = tran.KeyDeleteAsync(cacheKey);
//         }
//         
//         return await tran.ExecuteAsync();
//     }
//
//     public override async Task<bool> AddToCacheAsync<T>(string cacheKey, T data, Type[] types, CancellationToken cancellationToken = default) where T : class
//     {
//         var tran = Database.CreateTransaction();
//         foreach (var type in types)
//         {
//             _ = tran.SetAddAsync(GetTagKey(type), cacheKey);
//         }
//
//         _ = tran.StringSetAsync(cacheKey, data.SerializeToJson(), TimeSpan.FromMinutes(ExpirationInMinutes));
//         return await tran.ExecuteAsync();
//     }
//
//     public override async Task<ResponseInfo<T?>> GetFromCacheAsync<T>(string? cacheKey, CancellationToken cancellationToken = default) where T : class
//     {
//         var response = new ResponseInfo<T?>();
//         var resp = await Database.StringGetAsync(cacheKey);
//         string? json = resp;
//         return string.IsNullOrWhiteSpace(json) 
//             ? response.Fail() 
//             : response.Success(json.DeserializeFromJson<T>());
//     }
// }