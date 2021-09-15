using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.Identity
{
    public class SignInResponse<T> where T : IdentityUser
    {
        public T User { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsLockedOut { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public bool IsEnabled { get; set; }
    }
}