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
    public partial class AppSettingRepository : SolhigsonCachedRepositoryBase<Solhigson.Framework.Persistence.EntityModels.AppSetting
        ,Solhigson.Framework.Persistence.CacheModels.AppSettingCacheModel>, 
            Solhigson.Framework.Persistence.Repositories.Abstractions.IAppSettingRepository
    {
        public AppSettingRepository(Solhigson.Framework.Persistence.SolhigsonDbContext dbContext) : base(dbContext)
        {
        }

		public async Task<Solhigson.Framework.Persistence.EntityModels.AppSetting> GetByIdAsync(int id)
		{

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.AppSetting, bool>> query = 
				t => t.Id == id;
			return await GetByCondition(query).FirstOrDefaultAsync();
		}


		//Cached Methods
		public Solhigson.Framework.Persistence.CacheModels.AppSettingCacheModel GetByIdCached(int id)
		{

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.AppSetting, bool>> query = 
				t => t.Id == id;
			return GetSingleCached(query);
		}


    }
}