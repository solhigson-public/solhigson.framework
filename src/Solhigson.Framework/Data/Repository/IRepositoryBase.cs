using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Solhigson.Framework.Data.Repository;

public interface IRepositoryBase<T> where T : class, new()
{
    T New(object? identifier = null);
    IQueryable<T> Get(Expression<Func<T, bool>> expression);
    IQueryable<TK> Get<TK>(Expression<Func<T, bool>> expression) where TK : class;
    IQueryable<T> Where(Expression<Func<T, bool>> expression);
    IQueryable<TK> Where<TK>(Expression<Func<T, bool>> expression) where TK : class;
    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    T Add(T entity);
    void AddRange(IEnumerable<T> entities);
    Task<T> AddAndSaveChangesAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAndSaveChangesAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    T Attach(T entity);
    void AttachRange(IEnumerable<T> entities);
    T Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    Task<T> UpdateAndSaveChangesAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAndSaveChangesAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    T Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<T> RemoveAndSaveChangesAsync(T entity, CancellationToken cancellationToken = default);
    Task RemoveRangeAndSaveChangesAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
}