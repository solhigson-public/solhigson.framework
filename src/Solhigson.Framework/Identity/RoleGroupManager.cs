using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Identity
{
    public class RoleGroupManager<TRoleGroup, TRole, TUser, TContext> : IDisposable 
        where TRoleGroup : SolhigsonRoleGroup, new() 
        where TRole : SolhigsonAspNetRole 
        where TUser : IdentityUser
        where TContext : SolhigsonIdentityDbContext<TUser>
    {
        private readonly TContext _dbContext;
        public RoleGroupManager(TContext context)
        {
            _dbContext = context;
        }
        
        public virtual DbSet<TRoleGroup> RoleGroups => _dbContext.Set<TRoleGroup>();
        public virtual DbSet<TRole> Roles => _dbContext.Set<TRole>();
        
        public async Task<SolhigsonRoleGroup> CreateAsync(string roleGroupName)
        {
            return await CreateAsync(new TRoleGroup { Name = roleGroupName });
        }
        
        private async Task<SolhigsonRoleGroup> CreateAsync(TRoleGroup roleGroup)
        {
            if (roleGroup == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(roleGroup.Name))
            {
                throw new Exception($"Role group name is empty");
            }
            
            if (await RoleGroupExistsAsync(roleGroup.Name))
            {
                return roleGroup;
            }
            if (string.IsNullOrWhiteSpace(roleGroup.Id))
            {
                roleGroup.Id = Guid.NewGuid().ToString();
            }
            _dbContext.Add(roleGroup);
            await _dbContext.SaveChangesAsync();
            return roleGroup;
        }

        public async Task<bool> HasRoleGroups()
        {
            return await RoleGroups.AnyAsync();
        }

        public async Task<bool> RoleGroupExistsAsync(string roleGroupName)
        {
            return await _dbContext.RoleGroups.AnyAsync(t => t.Name == roleGroupName);
        }

        public async Task<SolhigsonRoleGroup> FindByNameAsync(string roleGroupName)
        {
            return await RoleGroups.FirstOrDefaultAsync(t => t.Name == roleGroupName);
        }

        public async Task<SolhigsonRoleGroup> FindByIdAsync(string id)
        {
            return await RoleGroups.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task DeleteAsync(TRoleGroup roleGroup)
        {
            if (roleGroup != null)
            {
                _dbContext.Remove(roleGroup);
                await _dbContext.SaveChangesAsync();
            }
        }
        
        public async Task UpdateAsync(TRoleGroup roleGroup)
        {
            if (roleGroup != null)
            {
                _dbContext.Update(roleGroup);
                await _dbContext.SaveChangesAsync();
            }
        }
        
        public async Task<IList<TRole>> GetRolesForGroupAsync(string roleGroupName)
        {
            return await Roles
                .Where(t => t.RoleGroup.Name == roleGroupName).ToListAsync();
        }

        public async Task AddRoleToGroupAsync(string roleName, string roleGroupName)
        {
            var roleGroup = await RoleGroups
                .FirstOrDefaultAsync(t => t.Name == roleGroupName);
            if (roleGroup is null)
            {
                throw new Exception($"RoleGroup: {roleGroupName} not found");
            }

            var role = await Roles.FirstOrDefaultAsync(t => t.Name == roleName);
            if (role is null)
            {
                throw new Exception($"Role: {roleName} not found");
            }

            role.RoleGroupId = roleGroup.Id;
            await _dbContext.SaveChangesAsync();
        }

        public bool RoleBelongsToGroupCached(string roleName, string roleGroupName)
        {
            return Roles.Include(t => t.RoleGroup).Where(t => t.Name == roleName).FromCacheSingle()
                ?.RoleGroup.Name == roleGroupName;
        }
        
        public string GetRoleGroupCached(string roleName)
        {
            return Roles.Include(t => t.RoleGroup).Where(t => t.Name == roleName).FromCacheSingle()
                ?.RoleGroup.Name;
        }


        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}