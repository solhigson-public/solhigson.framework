﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository
{
    public interface IRepositoryBase<T> where T : class, new()
    {
        T New();
        IQueryable<T> GetAll();
        IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression);
        bool ExistsWithCondition(Expression<Func<T, bool>> expression);
        T Add(T entity);
        void AddRange(IEnumerable<T> entities);
        T Attach(T entity);
        void AttachRange(IEnumerable<T> entities);
        T Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        T Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}