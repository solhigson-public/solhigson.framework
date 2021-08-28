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
    public partial interface IPermissionRepository 
        : Solhigson.Framework.Persistence.Repositories.Abstractions.ISolhigsonRepositoryBase<Solhigson.Framework.Persistence.EntityModels.Permission
            >
    {
		Task<Solhigson.Framework.Persistence.EntityModels.Permission> GetByIdAsync(string id);
		Task<Solhigson.Framework.Persistence.EntityModels.Permission> GetByNameAsync(string name);
    
    }
}