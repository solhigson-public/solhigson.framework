using Microsoft.AspNetCore.Http;

namespace Solhigson.Framework.Utilities;

public static class HttpUtils
{
    public static string UrlRoot(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            return string.Empty;
        }

        var scheme = httpContext.Request.Scheme;
        if (httpContext.Request.IsHttps && !scheme.Contains("s"))
        {
            scheme = "https";
        }

        return $"{scheme}://{httpContext.Request.Host}";
    }
}