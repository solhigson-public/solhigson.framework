---
name: API Standards (.NET)
description: "ASP.NET Core API conventions — ProblemDetails error responses (RFC 9457), URL path versioning with Asp.Versioning.Mvc, ApiVersion attributes"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
depends_on:
  - api-standards
---

# API Standards (.NET)

MUST use `ProblemDetails` middleware for standardized error responses (RFC 9457). MUST configure global exception handler to map exceptions to ProblemDetails. MUST use `Asp.Versioning.Mvc` with URL path versioning (`/api/v1/`). MUST register API versioning in DI. MUST apply `[ApiVersion]` attributes to all API controller classes.

For implementation patterns, MUST invoke the `dotnet-app` skill.
