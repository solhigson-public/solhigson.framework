using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonUser : IdentityUser
    {
        public bool IsEnabled { get; set; }
        public bool RequirePasswordChange { get; set; }
        public List<SolhigsonAspNetRole> Roles { get; set; }
    }
}