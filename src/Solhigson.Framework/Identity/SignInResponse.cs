using System;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity;

public class SignInResponse<T, TKey, TRole> 
    where T : SolhigsonUser<TKey, TRole> 
    where TRole : SolhigsonAspNetRole<TKey>
    where TKey : IEquatable<TKey>
{
    public T? User { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsLockedOut { get; set; }
    public bool RequiresTwoFactor { get; set; }
}