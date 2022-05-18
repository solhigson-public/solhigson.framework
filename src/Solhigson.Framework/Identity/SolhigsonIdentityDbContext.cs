using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.Identity;

public class SolhigsonIdentityDbContext<TUser> : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole, string> where TUser : SolhigsonUser
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
    
}
    
public class SolhigsonIdentityDbContext<TUser, TKey> : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole<TKey>, TKey> 
    where TUser : SolhigsonUser<TKey, SolhigsonAspNetRole<TKey>>
    where TKey : IEquatable<TKey>
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
        
}

    
public class SolhigsonIdentityDbContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey> 
    where TUser : SolhigsonUser<TKey, TRole> 
    where TRole : SolhigsonAspNetRole<TKey>
    where TKey : IEquatable<TKey>
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
    public DbSet<SolhigsonPermission> Permissions { get; set; }
    public DbSet<SolhigsonRolePermission<TKey>> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<SolhigsonRolePermission<TKey>>(b =>
        {
            b.HasKey(r => new { r.PermissionId, r.RoleId });
        });
        base.OnModelCreating(builder);
    }
}