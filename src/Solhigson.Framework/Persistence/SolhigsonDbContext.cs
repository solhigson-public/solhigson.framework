using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.Persistence
{
    public class SolhigsonDbContext : DbContext
    {
        public SolhigsonDbContext()
        {
            
        }
        public SolhigsonDbContext(DbContextOptions<SolhigsonDbContext> options)
            : base(options)
        {
            
        }

        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
    }
}