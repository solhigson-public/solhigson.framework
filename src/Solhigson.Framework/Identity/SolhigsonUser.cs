using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        public bool Enabled { get; set; }
        public bool RequirePasswordChange { get; set; }
        
        [NotMapped]
        public List<TRole> Roles { get; set; }

        private TRole _userRole;
        [NotMapped]
        public TRole UserRole
        {
            get => _userRole ??= Roles?.FirstOrDefault();
            set => _userRole = value;
        }
    }
}