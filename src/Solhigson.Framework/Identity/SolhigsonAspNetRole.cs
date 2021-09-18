using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonAspNetRole : SolhigsonAspNetRole<string>
    {
    }

    public class SolhigsonAspNetRole<T> : IdentityRole<T>, ICachedEntity where T : IEquatable<T>
    {
        [StringLength(450)]
        public string RoleGroupId { get; set; }
        
        [StringLength(450)]
        public string StartPage { get; set; }

        [ForeignKey(nameof(RoleGroupId))]
        public SolhigsonRoleGroup RoleGroup { get; set; }
    }
}