# Resilience Patterns — Detailed Reference

Retry and circuit breaker patterns for external service calls.

## Retry
- MUST use exponential backoff with jitter: `base * 2^attempt + random(0, jitterMax)`
- MUST configure per integration: max attempts, base delay, max delay, jitter range
- Non-retryable errors (4xx client errors, validation failures) MUST short-circuit immediately — MUST NOT retry
- Idempotent operations only — MUST NEVER retry non-idempotent calls without an idempotency key

## Circuit Breaker
- MUST track consecutive failures per external dependency
- MUST open circuit after failure threshold — reject calls immediately instead of waiting for timeouts
- MUST transition to half-open after cooldown period — allow a single probe request to test recovery
- MUST log circuit state transitions (Closed -> Open -> Half-Open -> Closed)

## Timeouts
- Every external call MUST have an explicit timeout — MUST NEVER allow unbounded waits
- MUST place timeout values in configuration (`appsettings.json` or equivalent); MUST NOT hardcode

## Configuration
- All retry, circuit breaker, and timeout settings MUST be externalized; MUST NEVER be hardcoded
- Each external integration MUST have its own resilience configuration
