using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Data
{
    public class SolhigsonCachedIdentityDbContext<T> : IdentityDbContext<T> where T : IdentityUser
    {
        public SolhigsonCachedIdentityDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            this.CheckAndUpdateCachedData();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            this.CheckAndUpdateCachedData();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.CheckAndUpdateCachedData();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new CancellationToken())
        {
            this.CheckAndUpdateCachedData();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}