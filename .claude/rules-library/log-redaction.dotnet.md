---
name: Log Redaction (.NET)
description: "PII log redaction — [PersonalData] attribute on DTOs/entities, Microsoft.Extensions.Compliance.Redaction DI config, structured logging with named parameters"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing monitoring or logging
  - writing code
depends_on:
  - log-redaction
---

# Log Redaction (.NET)

MUST annotate PII properties on DTOs/entities with `[PersonalData]` attribute. For local variables or service-level PII, use direct redaction via `Microsoft.Extensions.Compliance.Redaction` configuration. MUST configure redaction in DI for automatic log redaction. MUST use structured logging with named parameters — redaction operates on parameter values, not interpolated strings.

For implementation patterns, MUST invoke the `observability` skill.
