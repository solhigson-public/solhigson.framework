using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonIdentityManager<TUser, TContext> where TUser : IdentityUser where TContext : SolhigsonIdentityDbContext<TUser>
    {
        public UserManager<TUser> UserManager { get; }
        public RoleManager<SolhigsonAspNetRole> RoleManager { get; }
        private readonly TContext _dbContext;

        public SolhigsonIdentityManager(UserManager<TUser> userManager, RoleManager<SolhigsonAspNetRole> roleManager,
            IServiceProvider serviceProvider)
        {
            UserManager = userManager;
            RoleManager = roleManager;
            _dbContext = serviceProvider.GetRequiredService<TContext>();
        }

        public async Task<SolhigsonRoleGroup> CreateGroupAsync(string roleGroupName)
        {
            var group = new SolhigsonRoleGroup { Name = roleGroupName };
            _dbContext.Add(group);
            await _dbContext.SaveChangesAsync();
            return group;
        }

        public async Task<IList<SolhigsonAspNetRole>> GetRolesForGroupAsync(string roleGroupName)
        {
            return await _dbContext.Roles
                .Where(t => t.RoleGroup.Name == roleGroupName).ToListAsync();
        }

        public async Task<IdentityResult> CreateRoleAsync(string roleName, string roleGroupName = null)
        {
            string roleGroupId = null;
            if (!string.IsNullOrWhiteSpace(roleGroupName))
            {
                var roleGroup = await _dbContext.RoleGroups
                    .FirstOrDefaultAsync(t => t.Name == roleGroupName);
                if (roleGroup is null)
                {
                    throw new Exception($"RoleGroup: {roleName} not found");
                }

                roleGroupId = roleGroup.Id;
            }

            return await RoleManager.CreateAsync(new SolhigsonAspNetRole
            {
                Name = roleName,
                RoleGroupId = roleGroupId,
            });
        }

        public async Task AddRoleToGroupAsync(string roleName, string roleGroupName)
        {
            var roleGroup = await _dbContext.RoleGroups
                .FirstOrDefaultAsync(t => t.Name == roleGroupName);
            if (roleGroup is null)
            {
                throw new Exception($"RoleGroup: {roleGroupName} not found");
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(t => t.Name == roleName);
            if (role is null)
            {
                throw new Exception($"Role: {roleName} not found");
            }

            role.RoleGroupId = roleGroup.Id;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> HasRoleGroups()
        {
            return await _dbContext.RoleGroups.AnyAsync();
        }

        public bool RoleBelongsToGroupCached(string roleName, string roleGroupName)
        {
            return _dbContext.Roles.Include(t => t.RoleGroup).Where(t => t.Name == roleName).FromCacheSingle()
                ?.RoleGroup.Name == roleGroupName;
        }
        
        public string GetRoleGroupCached(string roleName)
        {
            return _dbContext.Roles.Include(t => t.RoleGroup).Where(t => t.Name == roleName).FromCacheSingle()
                ?.RoleGroup.Name;
        }
    }
}