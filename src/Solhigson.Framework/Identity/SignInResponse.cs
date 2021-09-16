using System;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity
{
    public class SignInResponse<T> : SignInResponse<T, string> where T :  SolhigsonUser<string>
    {
    }

    public class SignInResponse<T, TKey> where T : SolhigsonUser<TKey> where TKey : IEquatable<TKey>
    {
        public T User { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsLockedOut { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public bool IsEnabled { get; set; }
    }
}