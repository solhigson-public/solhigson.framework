using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity;

public interface IRoleGroupStore<TRoleGroup> : IDisposable where TRoleGroup : class
{
    /// <summary>
    /// Creates a new roleGroup in a store as an asynchronous operation.
    /// </summary>
    /// <param name="roleGroup">The roleGroup to create in the store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
    Task<IdentityResult> CreateAsync(TRoleGroup roleGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a roleGroup in a store as an asynchronous operation.
    /// </summary>
    /// <param name="roleGroup">The roleGroup to update in the store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
    Task<IdentityResult> UpdateAsync(TRoleGroup roleGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a roleGroup from the store as an asynchronous operation.
    /// </summary>
    /// <param name="roleGroup">The roleGroup to delete from the store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
    Task<IdentityResult> DeleteAsync(TRoleGroup roleGroup, CancellationToken cancellationToken);

    Task<IdentityResult> DeleteAsync(string roleGroupName, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the roleGroup who has the specified ID as an asynchronous operation.
    /// </summary>
    /// <param name="roleGroupId">The roleGroup ID to look for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
    Task<TRoleGroup> FindByIdAsync(string roleGroupId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the roleGroup who has the specified normalized name as an asynchronous operation.
    /// </summary>
    /// <param name="roleGroupName">The normalized roleGroup name to look for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
    Task<TRoleGroup> FindByNameAsync(string roleGroupName, CancellationToken cancellationToken);
}