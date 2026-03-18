using System;

namespace Solhigson.Framework.Throttling;

public sealed record ThrottleResult(bool Allowed, int Remaining, DateTimeOffset ResetUtc);
