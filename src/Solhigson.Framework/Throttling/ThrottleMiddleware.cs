using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Solhigson.Framework.Throttling;

public class ThrottleMiddleware(
    RequestDelegate next,
    IThrottleService throttleService,
    IOptions<ThrottleOptions> options,
    ILogger<ThrottleMiddleware> logger)
{
    private readonly ThrottleOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.GlobalEnabled || IsExcluded(context))
        {
            await next(context);
            return;
        }

        var key = $"global:{ThrottleKeyExtractor.GetClientKey(context)}";
        var window = TimeSpan.FromMinutes(1);

        try
        {
            var result = await throttleService.CheckAsync(
                key,
                _options.GlobalLimitPerMinute,
                window,
                cancellationToken: context.RequestAborted);

            if (!result.Allowed)
            {
                var retryAfter = Math.Max(0, (int)(result.ResetUtc - DateTimeOffset.UtcNow).TotalSeconds);
                context.Response.Headers.RetryAfter = retryAfter.ToString();
                context.Response.Headers["X-RateLimit-Limit"] = _options.GlobalLimitPerMinute.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] = result.ResetUtc.ToUnixTimeSeconds().ToString();
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $$"""{"error":"Too many requests","retryAfterSeconds":{{retryAfter}}}""",
                    cancellationToken: context.RequestAborted);
                return;
            }

            if (_options.IncludeHeadersOnSuccess)
            {
                context.Response.Headers["X-RateLimit-Limit"] = _options.GlobalLimitPerMinute.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = result.ResetUtc.ToUnixTimeSeconds().ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Global throttle check failed for key {Key}", key);
            if (!_options.FailOpen)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }
        }

        await next(context);
    }

    private bool IsExcluded(HttpContext context)
    {
        return _options.ExcludedPaths.Any(
            p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
