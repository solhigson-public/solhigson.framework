using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Data.Repository.Mocks
{
    public class MockedCachedRepositoryBase<T, TCacheModel> : MockRepositoryBase<T>, ICachedRepositoryBase<T, TCacheModel>
        where T : class, new() where TCacheModel : class
    {
        public IList<TCacheModel> GetListCached(Expression<Func<T, bool>> expression)
        {
            return Data.AsQueryable().Where(expression).ProjectToType<TCacheModel>().ToList();
        }

        public TCacheModel GetSingleCached(Expression<Func<T, bool>> expression)
        {
            return Data.AsQueryable().Where(expression).ProjectToType<TCacheModel>().FirstOrDefault();
        }
    }
}