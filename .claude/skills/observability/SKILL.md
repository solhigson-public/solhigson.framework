---
name: observability
description: Observability implementation for ASP.NET Core — OpenTelemetry, health checks, NLog, metrics.
---

# Observability Skill

For observability principles, see the common `observability` skill.

.NET-specific observability implementation patterns. No Aspire dependency — plain ASP.NET Core with OpenTelemetry SDK.

## When This Skill Is Invoked

- Setting up observability infrastructure for a new service
- Adding health checks for a new dependency
- Instrumenting custom business metrics
- Configuring structured logging with NLog
- Setting up distributed tracing

## Stack

- **Logging**: NLog with structured JSON output
- **Metrics & Tracing**: OpenTelemetry SDK (`OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`)
- **Health Checks**: `Microsoft.Extensions.Diagnostics.HealthChecks` + provider-specific packages
- **Correlation**: Custom middleware for propagating correlation IDs

## Key Conventions

- Health endpoints: `/health` (liveness), `/ready` (readiness with dependency checks)
- Custom metrics use `System.Diagnostics.Metrics` (Meter + Counter/Histogram)
- NLog targets configured per environment (console for dev, JSON file + external sink for prod)
- Alert thresholds in `appsettings.json`, not hardcoded

MUST follow `reference.md` for code templates and configuration examples.
