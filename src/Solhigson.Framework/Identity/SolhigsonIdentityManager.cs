using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonIdentityManager<TUser, TContext> :
        SolhigsonIdentityManager<TUser, SolhigsonRoleGroup, SolhigsonAspNetRole, TContext>, IDisposable
        where TUser : SolhigsonUser
        where TContext : SolhigsonIdentityDbContext<TUser>
    {
        public SolhigsonIdentityManager(UserManager<TUser> userManager, RoleManager<SolhigsonAspNetRole> roleManager,
            RoleGroupManager<SolhigsonRoleGroup, SolhigsonAspNetRole, TUser, TContext> roleGroupManager, SignInManager<TUser> signInManager,
            TContext dbContext) : base(userManager, roleManager, roleGroupManager, signInManager, dbContext)
        {
        }
    }

    public class SolhigsonIdentityManager<TUser, TRoleGroup, TRole, TContext> : IDisposable 
        where TUser : SolhigsonUser
        where TContext : SolhigsonIdentityDbContext<TUser> 
        where TRoleGroup : SolhigsonRoleGroup, new() 
        where TRole : SolhigsonAspNetRole
    {
        public RoleGroupManager<TRoleGroup, TRole, TUser, TContext> RoleGroupManager { get; }
        public UserManager<TUser> UserManager { get; }
        public RoleManager<SolhigsonAspNetRole> RoleManager { get; }
        public SignInManager<TUser> SignInManager { get; }
        private readonly SolhigsonIdentityDbContext<TUser>  _dbContext;

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

        public async Task SignOut()
        {
            await SignInManager.SignOutAsync();
        }
        
        public async Task<SignInResponse<TUser>> SignIn(string userName, string password, bool lockOutOnFailure = false)
        {
            var response = new SignInResponse<TUser>();
            var signInResponse = await SignInManager.PasswordSignInAsync(userName, password, false, lockOutOnFailure);
            response.IsSuccessful = signInResponse.Succeeded;
            response.IsLockedOut = signInResponse.IsLockedOut;
            response.RequiresTwoFactor = signInResponse.RequiresTwoFactor;
            if (!response.IsSuccessful)
            {
                return response;
            }
            
            response.User = await UserManager.FindByNameAsync(userName);
            var userRoles = _dbContext.UserRoles.Where(t => t.UserId == response.User.Id)
                .FromCacheList();
            
            if (userRoles.Any())
            {
                response.User.Roles = new List<SolhigsonAspNetRole>();
                foreach (var role in userRoles.Select(userRole => _dbContext.Roles.Where(t => t.Id == userRole.RoleId).FromCacheSingle()).Where(role => role != null))
                {
                    role.RoleGroup = _dbContext.RoleGroups.Where(t => t.Id == role.RoleGroupId).FromCacheSingle();
                    response.User.Roles.Add(role);
                }
            }
            return response;
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