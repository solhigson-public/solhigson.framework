using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data.Entities;

namespace Solhigson.Framework.Data
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
    }
}