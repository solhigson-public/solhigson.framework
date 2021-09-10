using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonIdentityManager<TUser, TContext> :
        SolhigsonIdentityManager<TUser, SolhigsonRoleGroup, SolhigsonAspNetRole, TContext>, IDisposable
        where TUser : IdentityUser
        where TContext : SolhigsonIdentityDbContext<TUser>
    {
        public SolhigsonIdentityManager(UserManager<TUser> userManager, RoleManager<SolhigsonAspNetRole> roleManager,
            RoleGroupManager<SolhigsonRoleGroup, SolhigsonAspNetRole, TUser, TContext> roleGroupManager, SignInManager<TUser> signInManager,
            TContext dbContext) : base(userManager, roleManager, roleGroupManager, signInManager, dbContext)
        {
        }
    }

    public class SolhigsonIdentityManager<TUser, TRoleGroup, TRole, TContext> : IDisposable 
        where TUser : IdentityUser
        where TContext : SolhigsonIdentityDbContext<TUser> 
        where TRoleGroup : SolhigsonRoleGroup, new() 
        where TRole : SolhigsonAspNetRole
    {
        public RoleGroupManager<TRoleGroup, TRole, TUser, TContext> RoleGroupManager { get; }
        public UserManager<TUser> UserManager { get; }
        public RoleManager<SolhigsonAspNetRole> RoleManager { get; }
        public SignInManager<TUser> SignInManager { get; }
        private readonly DbContext _dbContext;

        public SolhigsonIdentityManager(UserManager<TUser> userManager, RoleManager<SolhigsonAspNetRole> roleManager,
            RoleGroupManager<TRoleGroup, TRole, TUser, TContext> roleGroupManager, SignInManager<TUser> signInManager,
            TContext dbContext)
        {
            UserManager = userManager;
            RoleManager = roleManager;
            RoleGroupManager = roleGroupManager;
            SignInManager = signInManager;
            _dbContext = dbContext;
        }

        public async Task<IdentityResult> CreateRoleAsync(string roleName, string roleGroupName = null)
        {
            string roleGroupId = null;
            if (!string.IsNullOrWhiteSpace(roleGroupName))
            {
                var roleGroup = await _dbContext.Set<SolhigsonRoleGroup>()
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

        public void Dispose()
        {
            UserManager?.Dispose();
            RoleManager?.Dispose();
            RoleGroupManager?.Dispose();
            _dbContext?.Dispose();
        }
    }
}