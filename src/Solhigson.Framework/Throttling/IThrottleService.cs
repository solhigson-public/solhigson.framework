using System;
using System.Threading;
using System.Threading.Tasks;

namespace Solhigson.Framework.Throttling;

public interface IThrottleService
{
    Task<ThrottleResult> CheckAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
}
