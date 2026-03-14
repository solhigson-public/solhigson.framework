# Cooperative Cancellation (.NET)

MUST pass `CancellationToken` as the last parameter on all async service and repository methods. Controller actions MUST accept `CancellationToken` (ASP.NET Core binds from `HttpContext.RequestAborted` automatically). MUST pass token to all EF Core queries, `HttpClient` calls, and `Stream` operations. MUST use linked `CancellationTokenSource` for `Task.WhenAll` patterns (structured cancellation).

MUST ALWAYS pass `CancellationToken` using **named argument syntax** (`cancellationToken: ct` or `cancellationToken: cancellationToken`). Positional passing is PROHIBITED — methods with optional parameters before `CancellationToken` cause silent misrouting (e.g., CT assigned to `bool?`, `int`, or enum parameters).

MUST ALWAYS pass `CancellationToken` when a called method accepts one. MUST NOT call an async method without CT when a CT-accepting overload exists (CA2016). Exception: fire-and-forget operations (e.g., post-payment fulfillment) that MUST complete regardless of caller cancellation.

For detailed patterns, MUST invoke the `performance` skill.
