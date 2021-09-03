using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonAspNetRole : IdentityRole, ICachedEntity
    {
        public SolhigsonAspNetRole()
        {
            
        }
        public SolhigsonAspNetRole(string roleName, string roleGroupId) : base(roleName)
        {
            RoleGroupId = roleGroupId;
        }
        
        [StringLength(450)]
        public string RoleGroupId { get; set; }
        
        [ForeignKey(nameof(RoleGroupId))]
        public SolhigsonRoleGroup RoleGroup { get; set; }
    }
}