using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository;

public interface IRepositoryBase<T> where T : class, new()
{
    T New(object identifier = null);
    IQueryable<T> GetAll();
    IQueryable<T> Get(Expression<Func<T, bool>> expression);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
    T Add(T entity);
    void AddRange(IEnumerable<T> entities);
    Task<T> AddAndSaveChanges(T entity);
    Task AddRangeAndSaveChanges(IEnumerable<T> entities);
    T Attach(T entity);
    void AttachRange(IEnumerable<T> entities);
    T Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    Task<T> UpdateAndSaveChanges(T entity);
    Task UpdateRangeAndSaveChanges(IEnumerable<T> entities);

    T Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<T> RemoveAndSaveChanges(T entity);
    Task RemoveRangeAndSaveChanges(IEnumerable<T> entities);

}