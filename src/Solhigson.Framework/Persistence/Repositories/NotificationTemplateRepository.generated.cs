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
    public partial class NotificationTemplateRepository : SolhigsonCachedRepositoryBase<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate
        ,Solhigson.Framework.Persistence.CacheModels.NotificationTemplateCacheModel>, 
            Solhigson.Framework.Persistence.Repositories.Abstractions.INotificationTemplateRepository
    {
        public NotificationTemplateRepository(Solhigson.Framework.Persistence.SolhigsonDbContext dbContext) : base(dbContext)
        {
        }

		public async Task<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate> GetByIdAsync(int id)
		{

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate, bool>> query = 
				t => t.Id == id;
			return await Get(query).FirstOrDefaultAsync();
		}

		public async Task<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate> GetByNameAsync(string name)
		{
			if (name is null) { return null; }

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate, bool>> query = 
				t => t.Name == name;
			return await Get(query).FirstOrDefaultAsync();
		}


		//Cached Methods
		public Solhigson.Framework.Persistence.CacheModels.NotificationTemplateCacheModel GetByIdCached(int id)
		{

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate, bool>> query = 
				t => t.Id == id;
			return GetSingleCached(query);
		}

		public Solhigson.Framework.Persistence.CacheModels.NotificationTemplateCacheModel GetByNameCached(string name)
		{
			if (name is null) { return null; }

			Expression<Func<Solhigson.Framework.Persistence.EntityModels.NotificationTemplate, bool>> query = 
				t => t.Name == name;
			return GetSingleCached(query);
		}


    }
}