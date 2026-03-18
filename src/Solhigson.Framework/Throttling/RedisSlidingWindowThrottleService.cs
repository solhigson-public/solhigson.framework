using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Solhigson.Framework.Logging;
using StackExchange.Redis;

namespace Solhigson.Framework.Throttling;

public class RedisSlidingWindowThrottleService : IThrottleService
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(RedisSlidingWindowThrottleService).FullName);

    private const string KeyPrefix = "SolThrottle:";

    private const string LuaScript = """
        local key = KEYS[1]
        local windowSec = tonumber(ARGV[1])
        local limit = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])
        local member = ARGV[4]

        local windowStart = now - windowSec
        redis.call('ZREMRANGEBYSCORE', key, '-inf', windowStart)

        local count = redis.call('ZCARD', key)

        if count < limit then
          redis.call('ZADD', key, now, member)
          redis.call('EXPIRE', key, windowSec)
          return { 1, limit - count - 1, math.floor(now + windowSec) }
        else
          redis.call('EXPIRE', key, windowSec)
          local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
          local resetAt = oldest[2] and (tonumber(oldest[2]) + windowSec) or (now + windowSec)
          return { 0, 0, math.floor(resetAt) }
        end
        """;

    private readonly IDatabase _db;
    private readonly bool _failOpen;

    public RedisSlidingWindowThrottleService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<ThrottleOptions> options)
    {
        _db = connectionMultiplexer.GetDatabase();
        _failOpen = options.Value.FailOpen;
    }

    public async Task<ThrottleResult> CheckAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            var member = $"{now}:{Guid.NewGuid():N}";
            var windowSeconds = (long)window.TotalSeconds;

            var result = (RedisResult[]?)await _db.ScriptEvaluateAsync(
                LuaScript,
                [(RedisKey)$"{KeyPrefix}{key}"],
                [(RedisValue)windowSeconds, (RedisValue)limit, (RedisValue)now, (RedisValue)member]);

            if (result is { Length: 3 })
            {
                var allowed = (int)result[0] == 1;
                var remaining = (int)result[1];
                var resetEpoch = (long)result[2];
                var resetUtc = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);

                return new ThrottleResult(allowed, remaining, resetUtc);
            }

            Logger.LogWarning("Unexpected Lua script result for throttle key {Key}", key);
            return FailOpenResult();
        }
        catch (RedisException ex)
        {
            Logger.LogError(ex, "Redis throttle check failed for key {Key}", key);
            return _failOpen
                ? FailOpenResult()
                : new ThrottleResult(false, 0, DateTimeOffset.UtcNow.AddSeconds(60));
        }
    }

    private static ThrottleResult FailOpenResult()
    {
        return new ThrottleResult(true, 0, DateTimeOffset.UtcNow);
    }
}
