---
name: Observability
description: "Structured logging and health checks — named log parameters (no interpolation), /health and /ready endpoints, correlation ID propagation, dependency health coverage"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing monitoring or logging
  - writing deployment config
depends_on:
  - log-redaction
  - resilience
---

# Observability

## Health Checks

- **Liveness** (`/health`): MUST return healthy if the process is running. MUST NOT check dependencies — avoids cascading restarts.
- **Readiness** (`/ready`): MUST check all critical dependencies (database, cache, external APIs). MUST return dependency-level status with timing.

## Structured Logging

- MUST use named parameters — MUST NEVER use string interpolation for log fields
- MUST propagate a correlation ID across all service boundaries (inbound header → log context → outbound calls)
- MUST set correlation ID in the logging context (e.g., MDC/MDLC) so every log entry includes it automatically
- Log levels: `Debug` for development, `Info` for business events, `Warn` for recoverable issues, `Error` for failures requiring attention
- MUST NOT log PII, secrets, tokens, or API keys — see `log-redaction.md`

## Distributed Tracing

- MUST instrument inbound requests, outbound HTTP calls, and database queries as spans
- MUST propagate trace context (W3C `traceparent` header) across service boundaries
- Custom activity sources MUST use `{AppName}.*` naming convention
- MUST export traces to a configurable endpoint — MUST NOT hardcode collector URLs

## Metrics

- MUST instrument: request rate, error rate, p95/p99 latency, queue depth, active connections
- MUST add business metrics per domain (e.g., orders processed, payments settled)
- Custom meters MUST use `{AppName}.*` naming convention
- MUST export metrics to a configurable endpoint

## Alerting

- MUST define alert-worthy conditions: error rate spike, health check failure, latency degradation
- MUST externalize all alert thresholds in configuration — MUST NOT hardcode
- Circuit breaker state transitions MUST emit metrics for alerting
