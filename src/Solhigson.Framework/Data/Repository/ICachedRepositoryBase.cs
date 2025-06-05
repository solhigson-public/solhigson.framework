using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository;

public interface ICachedRepositoryBase<T, TCacheModel> : IRepositoryBase<T> where T : class, new() where TCacheModel : class
{
    Task<List<TCacheModel>> GetAllCachedAsync(CancellationToken cancellationToken = default);
    Task<List<TCacheModel>> GetListCachedAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    Task<TCacheModel?> GetSingleCachedAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    Task<List<TK>> GetListCachedAsync<TK>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default) where TK : class;
    Task<TK?> GetSingleCachedAsync<TK>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default) where TK : class;


}