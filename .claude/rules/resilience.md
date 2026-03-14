# Resilience

Retry, circuit breaker, and timeout patterns for external service calls.

- Every external integration MUST have resilience configuration (retry, circuit breaker, timeouts)
- All resilience settings MUST be externalized — MUST NEVER be hardcoded
- MUST classify dependencies as hard (request fails without it) vs soft (degraded but functional)
- MUST isolate dependency failure blast radius — named clients with concurrency limits (bulkhead pattern)
- MUST track circuit breaker state transitions as observable metrics
- For resilience implementation patterns, MUST invoke the `resilience` skill
