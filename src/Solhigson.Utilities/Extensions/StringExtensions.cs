namespace Solhigson.Utilities.Extensions;

public static class StringExtensions
{
    #region String
        
    public static string ToCamelCase(this string str) =>
        string.IsNullOrEmpty(str) || str.Length < 2
            ? str
            : char.ToLowerInvariant(str[0]) + str[1..];

    public static bool IsValidEmailAddress(this string email, bool ignoreEmpty = false)
    {
        return HelperFunctions.IsValidEmailAddress(email, ignoreEmpty);
    }
        
    public static bool IsValidPhoneNumber(this string phoneNumber, bool ignoreEmpty = false)
    {
        return HelperFunctions.IsValidPhoneNumber(phoneNumber, ignoreEmpty);
    }
    #endregion

}