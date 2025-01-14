#nullable enable
using System.Threading.Tasks;

namespace Solhigson.Framework.Persistence.Repositories.Abstractions
{
    /*
     * Generated by: Solhigson.Framework.efcoretool
     *
     * https://github.com/solhigson-public/solhigson.framework
     * https://www.nuget.org/packages/solhigson.framework.efcoretool
     *
     * This file is ALWAYS overwritten, DO NOT place custom code here
     */
    public partial interface IAppSettingRepository 
        : Solhigson.Framework.Persistence.Repositories.Abstractions.ISolhigsonCachedRepositoryBase<Solhigson.Framework.Persistence.EntityModels.AppSetting
            ,Solhigson.Framework.Persistence.CacheModels.AppSettingCacheModel>
    {
		Task<Solhigson.Framework.Persistence.EntityModels.AppSetting?> GetByIdAsync(int id);
		Task<Solhigson.Framework.Persistence.EntityModels.AppSetting?> GetByNameAsync(string name);

		//Cached Methods
		Task<Solhigson.Framework.Persistence.CacheModels.AppSettingCacheModel?> GetByIdCachedAsync(int id);
		Task<Solhigson.Framework.Persistence.CacheModels.AppSettingCacheModel?> GetByNameCachedAsync(string name);
    
    }
}