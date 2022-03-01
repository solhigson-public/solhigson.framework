using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Web.Hangfire;

public class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly BasicAuthAuthorizationFilterOptions _options;

    public BasicAuthAuthorizationFilter()
        : this(new BasicAuthAuthorizationFilterOptions())
    {
    }

    public BasicAuthAuthorizationFilter(BasicAuthAuthorizationFilterOptions options)
    {
        _options = options;
    }

    public bool Authorize(DashboardContext dashboardContext)
    {
        var context = dashboardContext.GetHttpContext();
        /*
        if (_options.SslRedirect && context.Request.Scheme != "https")
        {
            var redirectUri = new UriBuilder("https", context.Request.Host.ToString(), 443, context.Request.Path)
                .ToString();

            context.Response.StatusCode = 301;
            context.Response.Redirect(redirectUri);
            return false;
        }
        */

        if (_options.RequireSsl && context.Request.IsHttps == false)
        {
            return false;
        }

        string header = context.Request.Headers["Authorization"];

        if (string.IsNullOrWhiteSpace(header))
        {
            return Challenge(context);
        }

        var authValues = AuthenticationHeaderValue.Parse(header);

        if (!"Basic".Equals(authValues.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return Challenge(context);
        }

        var parameter = Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter));
        var parts = parameter.Split(':');

        if (parts.Length <= 1)
        {
            return Challenge(context);
        }

        var login = parts[0];
        var password = parts[1];

        if (!string.IsNullOrWhiteSpace(login) &&
            !string.IsNullOrWhiteSpace(password))
        {
            return _options
                       .Users
                       .Any(user => user.Validate(login, password, _options.LoginCaseSensitive))
                   || Challenge(context);
        }

        return Challenge(context);
    }

    private static bool Challenge(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
        return false;
    }

    public static BasicAuthAuthorizationFilter Default(string password,
        bool requireSsl = false)
    {
        return new BasicAuthAuthorizationFilter(
            new BasicAuthAuthorizationFilterOptions
            {
                RequireSsl = requireSsl,
                LoginCaseSensitive = true,
                Users = new[]
                {
                    new BasicAuthAuthorizationUser
                    {
                        Login = "admin",
                        PasswordClear = password
                    }
                }
            });
    }
}