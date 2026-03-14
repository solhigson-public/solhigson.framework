# API Standards (.NET)

MUST use `ProblemDetails` middleware for standardized error responses (RFC 9457). MUST configure global exception handler to map exceptions to ProblemDetails. MUST use `Asp.Versioning.Mvc` with URL path versioning (`/api/v1/`). MUST register API versioning in DI and apply `[ApiVersion]` attributes.

For implementation patterns, MUST invoke the `dotnet-app` skill.
