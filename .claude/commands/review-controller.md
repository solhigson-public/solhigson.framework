---
description: Review an ASP.NET Core controller for conventions and best practices.
---

Review this controller for compliance with project conventions.

## Governed By

- `dotnet-conventions.md` — naming, base classes, file-scoped namespaces
- `permissions-pattern.md` — auth attribute enforcement
- `facade-service-pattern.md` — service delegation, no inline business logic
- `service-patterns.md` — service call patterns
- `performance.md` — defensive programming, secure coding (OWASP)

## Procedure

1. **Base class** — MUST verify correct base class (`MvcBaseController` or `ApiBaseController`) and folder placement (`Controllers/Mvc/` or `Controllers/Api/`).

2. **Thin controller** — MUST verify no business logic in actions. MUST verify all data access goes through services via `ServicesWrapper`, NEVER directly via `DbContext` or `RepositoryWrapper`.

3. **Auth attributes** — MUST verify every action has exactly one of: `[AllowAnonymous]`, `[Permission(Permission.X)]`, or `[Authorize]`. MUST verify throttling attributes on mutation and search endpoints.

4. **Route conventions** — MUST verify route attributes on every action. MUST verify RESTful HTTP methods (GET for reads, POST for mutations).

5. **Patterns** — MUST verify `SessionUser` for current user context (MUST NOT use `HttpContext.User` directly). MUST verify `SetErrorMessage()`/`SetInfoMessage()` for flash messages (MUST NOT use `TempData` directly).

6. **OWASP compliance** — MUST verify input validation, no raw SQL/string concatenation, output encoding, authorization before data access.

MUST flag any violations and MUST suggest fixes with code examples.
