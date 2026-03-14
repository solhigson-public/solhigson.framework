# Observability

MUST use structured logging with named parameters — MUST NEVER use string interpolation for log fields. Every deployed service MUST expose `/health` (liveness) and `/ready` (readiness). MUST propagate correlation IDs across service boundaries.

Health checks MUST cover all critical dependencies (database, cache, storage, job scheduler, disk, memory). For circuit breaker observability, MUST comply with `resilience.md`.

For implementation patterns (OpenTelemetry, health checks, NLog, metrics, tracing, alerting), MUST invoke the `observability` skill. For defensive logging (no PII, no secrets), MUST comply with `log-redaction.md`.
