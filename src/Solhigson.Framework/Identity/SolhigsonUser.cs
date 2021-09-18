using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonUser : SolhigsonUser<string>
    {
        public SolhigsonUser()
        {
            Id = Guid.NewGuid().ToString();
            SecurityStamp = Guid.NewGuid().ToString();
        }
    }

    public class SolhigsonUser<T> : IdentityUser<T> where T : IEquatable<T>
    {
        public bool IsEnabled { get; set; }
        public bool RequirePasswordChange { get; set; }
        public List<SolhigsonAspNetRole<T>> Roles { get; set; }
    }
}