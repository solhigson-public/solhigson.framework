using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Identity;
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
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    }
    

}