# API Standards

MUST return standardized error responses following RFC 9457 (ProblemDetails shape) on all API endpoints. MUST version API endpoints explicitly — URL path versioning (`/api/v1/`). MUST NOT expose internal error details in production error responses.

For implementation patterns, MUST invoke the `dotnet-app` skill (dotnet) or equivalent.
