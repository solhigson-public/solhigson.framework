using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.EfCore;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Identity;

public class RoleGroupManager<TRoleGroup, TRole, TUser, TContext, TKey>(TContext context) : IDisposable
    where TRoleGroup : SolhigsonRoleGroup, new()
    where TRole : SolhigsonAspNetRole<TKey>
    where TUser : SolhigsonUser<TKey, TRole>
    where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey>
    where TKey : IEquatable<TKey>
{
    protected virtual DbSet<TRoleGroup> RoleGroups => context.Set<TRoleGroup>();
    protected virtual DbSet<TRole> Roles => context.Set<TRole>();
        
    public async Task<SolhigsonRoleGroup?> CreateAsync(string roleGroupName, CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new TRoleGroup { Name = roleGroupName }, cancellationToken);
    }
        
    private async Task<SolhigsonRoleGroup?> CreateAsync(TRoleGroup? roleGroup, CancellationToken cancellationToken = default)
    {
        if (roleGroup == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(roleGroup.Name))
        {
            throw new Exception($"Role group name is empty");
        }
            
        if (await RoleGroupExistsAsync(roleGroup.Name, cancellationToken))
        {
            return roleGroup;
        }
        if (string.IsNullOrWhiteSpace(roleGroup.Id))
        {
            roleGroup.Id = Guid.NewGuid().ToString();
        }
        context.Add(roleGroup);
        await context.SaveChangesAsync(cancellationToken);
        return roleGroup;
    }

    public async Task<bool> HasRoleGroups(CancellationToken cancellationToken = default)
    {
        return await RoleGroups.AnyAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> RoleGroupExistsAsync(string roleGroupName, CancellationToken cancellationToken = default)
    {
        return await context.RoleGroups.AnyAsync(t => t.Name == roleGroupName, cancellationToken: cancellationToken);
    }

    public async Task<SolhigsonRoleGroup?> FindByNameAsync(string roleGroupName, CancellationToken cancellationToken = default)
    {
        return await RoleGroups.FirstOrDefaultAsync(t => t.Name == roleGroupName, cancellationToken: cancellationToken);
    }

    public async Task<SolhigsonRoleGroup?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await RoleGroups.FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(TRoleGroup? roleGroup, CancellationToken cancellationToken = default)
    {
        if (roleGroup != null)
        {
            context.Remove(roleGroup);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
        
    public async Task UpdateAsync(TRoleGroup? roleGroup, CancellationToken cancellationToken = default)
    {
        if (roleGroup != null)
        {
            context.Update(roleGroup);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
        
    public async Task<IList<TRole>> GetRolesForGroupAsync(string roleGroupName, CancellationToken cancellationToken = default)
    {
        return await Roles
            .Where(t => t.RoleGroup != null && t.RoleGroup.Name == roleGroupName).ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task AddRoleToGroupAsync(string roleName, string roleGroupName, CancellationToken cancellationToken = default)
    {
        var roleGroup = await RoleGroups
            .FirstOrDefaultAsync(t => t.Name == roleGroupName, cancellationToken: cancellationToken);
        if (roleGroup is null)
        {
            throw new Exception($"RoleGroup: {roleGroupName} not found");
        }

        var role = await Roles.FirstOrDefaultAsync(t => t.Name == roleName, cancellationToken: cancellationToken);
        if (role is null)
        {
            throw new Exception($"Role: {roleName} not found");
        }

        role.RoleGroupId = roleGroup.Id;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RoleBelongsToGroupCachedAsync(string roleName, string roleGroupName, CancellationToken cancellationToken = default)
    {
        return (await Roles.Include(t => t.RoleGroup).Where(t => t.Name == roleName).FromCacheSingleAsync(cancellationToken: cancellationToken))
            ?.RoleGroup!.Name == roleGroupName;
    }
        
    public async Task<string?> GetRoleGroupCached(string roleName, CancellationToken cancellationToken = default)
    {
        return (await Roles.Include(t => t.RoleGroup).Where(t => t.Name == roleName).FromCacheSingleAsync(cancellationToken: cancellationToken))
            ?.RoleGroup!.Name;
    }


    public void Dispose()
    {
        context.Dispose();
    }
}