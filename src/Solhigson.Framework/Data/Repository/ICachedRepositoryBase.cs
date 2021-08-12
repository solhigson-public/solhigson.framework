using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Solhigson.Framework.Data.Repository
{
    public interface ICachedRepositoryBase<T, TCacheModel> : IRepositoryBase<T> where T : class, new() where TCacheModel : class
    {
        IList<TCacheModel> GetListCached(Expression<Func<T, bool>> expression);
        TCacheModel GetSingleCached(Expression<Func<T, bool>> expression);

    }
}