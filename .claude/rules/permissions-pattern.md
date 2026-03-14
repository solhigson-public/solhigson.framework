# Permissions & RBAC Pattern

Every controller action MUST have exactly one of: `[AllowAnonymous]`, `[Permission(Permission.X)]`, or `[Authorize]`. Actions without any auth attribute are security bugs. MUST NEVER hardcode permission strings — MUST ALWAYS use generated `Permission.*` constants.

For permission definition, controller usage, enforcement (build-time + runtime), antiforgery, and service call counting, MUST invoke the `dotnet-app` skill.
