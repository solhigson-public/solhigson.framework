using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Persistence.Repositories
{
    /*
     * Generated by: Solhigson.Framework.efcoretool
     *
     * https://github.com/solhigson-public/solhigson.framework
     * https://www.nuget.org/packages/solhigson.framework.efcoretool
     *
     * This file is ALWAYS overwritten, DO NOT place custom code here
     */
    public partial class PermissionRepository : SolhigsonCachedRepositoryBase<Solhigson.Framework.Persistence.EntityModels.Permission
        ,Solhigson.Framework.Persistence.CacheModels.PermissionCacheModel>, 
            Solhigson.Framework.Persistence.Repositories.Abstractions.IPermissionRepository
    {
        public PermissionRepository(Solhigson.Framework.Persistence.SolhigsonDbContext dbContext) : base(dbContext)
        {
        }

		public async Task<Solhigson.Framework.Persistence.EntityModels.Permission> GetByIdAsync(string id)
		{
			if (id is null) { return null; }

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.Permission, bool>> query = 
				t => t.Id == id;
			return await Get(query).FirstOrDefaultAsync();
		}

		public async Task<Solhigson.Framework.Persistence.EntityModels.Permission> GetByNameAsync(string name)
		{
			if (name is null) { return null; }

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.Permission, bool>> query = 
				t => t.Name == name;
			return await Get(query).FirstOrDefaultAsync();
		}


		//Cached Methods
		public Solhigson.Framework.Persistence.CacheModels.PermissionCacheModel GetByIdCached(string id)
		{
			if (id is null) { return null; }

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.Permission, bool>> query = 
				t => t.Id == id;
			return GetSingleCached(query);
		}

		public Solhigson.Framework.Persistence.CacheModels.PermissionCacheModel GetByNameCached(string name)
		{
			if (name is null) { return null; }

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.Permission, bool>> query = 
				t => t.Name == name;
			return GetSingleCached(query);
		}


    }
}