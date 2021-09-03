using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonIdentityDbContext<T> : IdentityDbContext<T, SolhigsonAspNetRole, string> where T : IdentityUser
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SolhigsonIdentityDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public SolhigsonIdentityDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolhigsonIdentityDbContext" /> class.
        /// </summary>
        protected SolhigsonIdentityDbContext() { }
        
        public DbSet<SolhigsonRoleGroup> RoleGroups { get; set; }
    }
}