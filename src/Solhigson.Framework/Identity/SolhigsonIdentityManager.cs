﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Identity
{
    public class SolhigsonIdentityManager<TUser, TContext> :
        SolhigsonIdentityManager<TUser, SolhigsonRoleGroup, SolhigsonAspNetRole, TContext, string>
        where TUser : SolhigsonUser
        where TContext : SolhigsonIdentityDbContext<TUser>
    {
        public SolhigsonIdentityManager(UserManager<TUser> userManager, RoleManager<SolhigsonAspNetRole> roleManager,
            RoleGroupManager<SolhigsonRoleGroup, SolhigsonAspNetRole, TUser, TContext, string> roleGroupManager, SignInManager<TUser> signInManager,
            PermissionManager<TUser, SolhigsonAspNetRole, TContext, string> permissionManager, TContext dbContext) 
            : base(userManager, roleManager, roleGroupManager, signInManager, permissionManager, dbContext)
        {
        }
    }

    public class SolhigsonIdentityManager<TUser, TRoleGroup, TRole, TContext, TKey> : IDisposable 
        where TUser : SolhigsonUser<TKey>
        where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey> 
        where TRoleGroup : SolhigsonRoleGroup, new() 
        where TRole : SolhigsonAspNetRole<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        public RoleGroupManager<TRoleGroup, TRole, TUser, TContext, TKey> RoleGroupManager { get; }
        public UserManager<TUser> UserManager { get; }
        public RoleManager<TRole> RoleManager { get; }
        public SignInManager<TUser> SignInManager { get; }
        public PermissionManager<TUser, TRole, TContext, TKey> PermissionManager { get; }
        private readonly SolhigsonIdentityDbContext<TUser, TRole, TKey>  _dbContext;

        public SolhigsonIdentityManager(UserManager<TUser> userManager, RoleManager<TRole> roleManager,
            RoleGroupManager<TRoleGroup, TRole, TUser, TContext, TKey> roleGroupManager, SignInManager<TUser> signInManager,
            PermissionManager<TUser, TRole, TContext, TKey> permissionManager, TContext dbContext)
        {
            UserManager = userManager;
            RoleManager = roleManager;
            RoleGroupManager = roleGroupManager;
            SignInManager = signInManager;
            PermissionManager = permissionManager;
            _dbContext = dbContext;
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

            return await RoleManager.CreateAsync(new TRole
            {
                Name = roleName,
                RoleGroupId = roleGroupId,
            });
        }

        public async Task SignOut()
        {
            await SignInManager.SignOutAsync();
        }
        
        public async Task<SignInResponse<TUser, TKey>> SignIn(string userName, string password, bool lockOutOnFailure = false)
        {
            var response = new SignInResponse<TUser, TKey>();
            var signInResponse = await SignInManager.PasswordSignInAsync(userName, password, false, lockOutOnFailure);
            response.IsSuccessful = signInResponse.Succeeded;
            response.IsLockedOut = signInResponse.IsLockedOut;
            response.RequiresTwoFactor = signInResponse.RequiresTwoFactor;
            if (!response.IsSuccessful)
            {
                return response;
            }
            
            response.User = await UserManager.FindByNameAsync(userName);
            var userRoles = _dbContext.UserRoles.Where(t => t.UserId.Equals(response.User.Id))
                .FromCacheList();
            
            if (userRoles.Any())
            {
                response.User.Roles = new List<SolhigsonAspNetRole<TKey>>();
                foreach (var role in userRoles.Select(userRole => _dbContext.Roles.Where(t => t.Id.Equals(userRole.RoleId)).FromCacheSingle()).Where(role => role != null))
                {
                    role.RoleGroup = _dbContext.RoleGroups.Where(t => t.Id == role.RoleGroupId).FromCacheSingle();
                    response.User.Roles.Add(role);
                }
            }
            return response;
        }

        public async Task<List<T>> GetUsersInRoles<T>(string[] roles)
        {
            return await (from user in _dbContext.Users
                join userRole in _dbContext.UserRoles
                    on user.Id equals userRole.UserId
                join role in _dbContext.Roles
                    on userRole.RoleId equals role.Id
                where roles.Contains(role.Name)
                && user.IsEnabled
                select user).ProjectToType<T>().ToListAsync();
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