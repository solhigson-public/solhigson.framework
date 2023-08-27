using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository;

public interface IRepositoryBase<T> where T : class, new()
{
    IQueryable<T> Get(Expression<Func<T, bool>> expression);
    IQueryable<TK> Get<TK>(Expression<Func<T, bool>> expression) where TK : class;
    IQueryable<T> Where(Expression<Func<T, bool>> expression);
    IQueryable<TK> Where<TK>(Expression<Func<T, bool>> expression) where TK : class;
    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
    void AddRange(IEnumerable<T> entities);
    Task<T> AddAndSaveChangesAsync(T entity);
    Task AddRangeAndSaveChangesAsync(IEnumerable<T> entities);
    T Attach(T entity);
    void AttachRange(IEnumerable<T> entities);
    T Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    Task<T> UpdateAndSaveChangesAsync(T entity);
    Task UpdateRangeAndSaveChangesAsync(IEnumerable<T> entities);

    T Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<T> RemoveAndSaveChangesAsync(T entity);
    Task RemoveRangeAndSaveChangesAsync(IEnumerable<T> entities);

}