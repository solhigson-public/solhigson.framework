using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.EfCore;

namespace Solhigson.Framework.Identity;

public class SolhigsonIdentityManager<TUser, TContext>(
    UserManager<TUser> userManager,
    RoleManager<SolhigsonAspNetRole> roleManager,
    RoleGroupManager<SolhigsonRoleGroup, SolhigsonAspNetRole, TUser, TContext, string> roleGroupManager,
    SignInManager<TUser> signInManager,
    PermissionManager<TUser, SolhigsonAspNetRole, TContext, string> permissionManager,
    TContext dbContext)
    : SolhigsonIdentityManager<TUser, SolhigsonRoleGroup, SolhigsonAspNetRole, TContext, string>(userManager,
        roleManager, roleGroupManager, signInManager, permissionManager,
        dbContext)
    where TUser : SolhigsonUser<string, SolhigsonAspNetRole>
    where TContext : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole, string>;
    
public class SolhigsonIdentityManager<TUser, TKey, TContext>(
    UserManager<TUser> userManager,
    RoleManager<SolhigsonAspNetRole<TKey>> roleManager,
    RoleGroupManager<SolhigsonRoleGroup, SolhigsonAspNetRole<TKey>, TUser, TContext, TKey> roleGroupManager,
    SignInManager<TUser> signInManager,
    PermissionManager<TUser, SolhigsonAspNetRole<TKey>, TContext, TKey> permissionManager,
    TContext dbContext)
    : SolhigsonIdentityManager<TUser, SolhigsonRoleGroup, SolhigsonAspNetRole<TKey>, TContext, TKey>(userManager,
        roleManager, roleGroupManager, signInManager, permissionManager, dbContext)
    where TUser : SolhigsonUser<TKey, SolhigsonAspNetRole<TKey>>
    where TContext : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole<TKey>, TKey>
    where TKey : IEquatable<TKey>;


public abstract class SolhigsonIdentityManager<TUser, TRoleGroup, TRole, TContext, TKey>(
    UserManager<TUser> userManager,
    RoleManager<TRole> roleManager,
    RoleGroupManager<TRoleGroup, TRole, TUser, TContext, TKey> roleGroupManager,
    SignInManager<TUser> signInManager,
    PermissionManager<TUser, TRole, TContext, TKey> permissionManager,
    TContext dbContext)
    : IDisposable
    where TUser : SolhigsonUser<TKey, TRole>
    where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey>
    where TRoleGroup : SolhigsonRoleGroup, new()
    where TRole : SolhigsonAspNetRole<TKey>, new()
    where TKey : IEquatable<TKey>
{
    public RoleGroupManager<TRoleGroup, TRole, TUser, TContext, TKey> RoleGroupManager { get; } = roleGroupManager;
    public UserManager<TUser> UserManager { get; } = userManager;
    public RoleManager<TRole> RoleManager { get; } = roleManager;
    public SignInManager<TUser> SignInManager { get; } = signInManager;
    public PermissionManager<TUser, TRole, TContext, TKey> PermissionManager { get; } = permissionManager;
    private readonly SolhigsonIdentityDbContext<TUser, TRole, TKey>  _dbContext = dbContext;

    public async Task<IdentityResult> CreateRoleAsync(string roleName, string? roleGroupName = null, CancellationToken cancellationToken = default)
    {
        string roleGroupId = null!;
        if (!string.IsNullOrWhiteSpace(roleGroupName))
        {
            var roleGroup = await _dbContext.RoleGroups
                .FirstOrDefaultAsync(t => t.Name == roleGroupName, cancellationToken: cancellationToken);
            if (roleGroup is null)
            {
                throw new Exception($"RoleGroup: {roleName} not found");
            }

            roleGroupId = roleGroup.Id;
        }
        
        var existingRole = await RoleManager.FindByNameAsync(roleName);
        if (existingRole is not null)
        {
            return IdentityResult.Success;
        }

        var role = new TRole
        {
            Name = roleName,
            RoleGroupId = roleGroupId,
        };
        return await RoleManager.CreateAsync(role);
    }

    public async Task SignOutAsync()
    {
        await SignInManager.SignOutAsync();
    }
        
    public async Task<TUser?> GetUserDetailsByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await UserManager.FindByIdAsync(id);
        await GetRolesAsync(user, cancellationToken);
        return user;
    }


    public async Task<TUser?> GetUserDetailsByUsernameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var user = await UserManager.FindByNameAsync(userName);
        await GetRolesAsync(user, cancellationToken);
        return user;
    }

    private async Task GetRolesAsync(TUser? user, CancellationToken cancellationToken = default)
    {
        if (user is null)
        {
            return;
        }
        var userRoles = await _dbContext.UserRoles.Where(t => t.UserId.Equals(user.Id))
            .FromCacheListAsync(cancellationToken: cancellationToken);
            
        if (userRoles.Any())
        {
            user.Roles = new List<TRole>();
            foreach (var role in userRoles.Select(userRole => _dbContext.Roles.Where(t => t.Id.Equals(userRole.RoleId)).FromCacheSingleAsync(cancellationToken: cancellationToken).Result).Where(role => role is not null))
            {
                if (role is null)
                {
                    continue;
                }
                role.RoleGroup = await _dbContext.RoleGroups.Where(t => t.Id == role.RoleGroupId).FromCacheSingleAsync(cancellationToken: cancellationToken);
                user.Roles.Add(role);
            }
        }
    }
        
    public async Task<TUser?> GetUserDetailsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await UserManager.FindByEmailAsync(email);
        await GetRolesAsync(user, cancellationToken);
        return user;
    }

        
    public async Task<SignInResponse<TUser, TKey, TRole>> SignInAsync(string userName, string password, bool lockOutOnFailure = false, CancellationToken cancellationToken = default)
    {
        var response = new SignInResponse<TUser, TKey, TRole>();
        var signInResponse = await SignInManager.PasswordSignInAsync(userName, password, false, lockOutOnFailure);
        response.IsSuccessful = signInResponse.Succeeded;
        response.IsLockedOut = signInResponse.IsLockedOut;
        response.RequiresTwoFactor = signInResponse.RequiresTwoFactor;
        if (!response.IsSuccessful)
        {
            return response;
        }
            
        response.User = await GetUserDetailsByUsernameAsync(userName, cancellationToken);
        if (response.User is null)
        {
            response.IsSuccessful = false;
        }
        return response;
    }

    public async Task UpdateLastLoginTimeAsync(string userName, DateTime? lastLoginTime = null, CancellationToken cancellationToken = default)
    {
        var user = await UserManager.FindByNameAsync(userName);
        if (user is null)
        {
            return;
        }

        user.LastLoginDate = lastLoginTime ?? DateTime.UtcNow;
        await UserManager.UpdateAsync(user);
    }

    public async Task<List<T>> GetUsersInRolesAsync<T>(string[] roles, CancellationToken cancellationToken = default)
    {
        return await (from user in _dbContext.Users
            join userRole in _dbContext.UserRoles
                on user.Id equals userRole.UserId
            join role in _dbContext.Roles
                on userRole.RoleId equals role.Id
            where roles.Contains(role.Name)
                  && user.Enabled
            select user).ProjectToType<T>().ToListAsync(cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        UserManager.Dispose();
        RoleManager.Dispose();
        RoleGroupManager.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}