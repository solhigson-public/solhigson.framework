using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonUser : SolhigsonUser<string, SolhigsonAspNetRole>
    {
        public SolhigsonUser()
        {
            Id = Guid.NewGuid().ToString();
            SecurityStamp = Guid.NewGuid().ToString();
        }
    }
    public class SolhigsonUser<T> : SolhigsonUser<T, SolhigsonAspNetRole<T>> where T : IEquatable<T> 
    {
    }

    public class SolhigsonUser<T, TRole> : IdentityUser<T> where T : IEquatable<T> 
        where TRole : SolhigsonAspNetRole<T>
    {
        public bool IsEnabled { get; set; }
        public bool RequirePasswordChange { get; set; }
        
        [NotMapped]
        public List<TRole> Roles { get; set; }
    }
}