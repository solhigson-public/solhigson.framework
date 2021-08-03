using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solhigson.Framework.Data.Repository
{
    public interface ICachedRepositoryBase<T> : IRepositoryBase<T> where T : ICachedEntity
    {
        IList<T> GetAllCached();
        T GetByConditionCached(Expression<Func<T, bool>> expression);
    }
    
}