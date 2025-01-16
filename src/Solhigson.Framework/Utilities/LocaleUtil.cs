using Microsoft.AspNetCore.Http;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Utilities;

public static class LocaleUtil
{
       
    public static string T(string val)
    {
        return val;
    }
        
    public static int GetTimeZoneOffset()
    {
        var timeOffSet = HelperFunctions.SafeGetSessionData(Constants.TimeZoneCookieName,
            ServiceProviderWrapper.GetHttpContextAccessor()) ?? ServiceProviderWrapper.GetHttpContextAccessor()?.HttpContext?.Request?.Cookies[Constants.TimeZoneCookieName];

        if (timeOffSet != null && int.TryParse(timeOffSet, out var offset))
        {
            return offset;
        }
        return 0;
    }

}