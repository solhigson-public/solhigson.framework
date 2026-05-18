---
name: API Standards
description: "API endpoint conventions — RFC 9457 ProblemDetails errors, URL path versioning, production error sanitization"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
---

# API Standards

MUST return standardized error responses following RFC 9457 (ProblemDetails shape) on all API endpoints. MUST version API endpoints explicitly — URL path versioning (`/api/v1/`). MUST NOT expose internal error details in production error responses.

For implementation patterns, MUST invoke the `dotnet-app` skill (dotnet) or equivalent.
