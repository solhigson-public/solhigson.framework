using System.Collections.Generic;

namespace Solhigson.Framework.Throttling;

public class ThrottleOptions
{
    public bool GlobalEnabled { get; set; } = true;
    public int GlobalLimitPerMinute { get; set; } = 1000;
    public bool FailOpen { get; set; } = true;
    public bool IncludeHeadersOnSuccess { get; set; }
    public HashSet<string> ExcludedPaths { get; set; } = ["/health", "/alive", "/ready"];
}
