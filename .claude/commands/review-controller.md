---
description: Review an ASP.NET Core controller for conventions and best practices.
---

Review this controller for compliance with project conventions:

## Base Class & Inheritance
- MVC controllers must inherit `MvcBaseController` (extends `SolhigsonMvcControllerBase`)
- API controllers must inherit `ApiBaseController` (extends `SolhigsonApiControllerBase`)
- Controller must be in correct folder: `Controllers/Mvc/` or `Controllers/Api/`

## Thin Controller Principle
- No business logic in the controller — delegate to services via `ServicesWrapper`
- Controller actions should: validate input, call service, return result
- No direct `DbContext` or `RepositoryWrapper` access — use services

## Route & HTTP Conventions
- Route attributes on every action: `[HttpGet("...")]`, `[HttpPost("...")]`
- RESTful HTTP methods: GET for reads, POST for mutations
- MVC returns `View()`, `RedirectToAction()`, or `Redirect()`
- API returns `ResponseInfo<T>` via base controller helpers

## Auth & Security
- Every action must have `[AllowAnonymous]` or `[Permission("...")]`
- Throttling attributes where appropriate: `[ThrottleByParam]`, `[ThrottleByUser]`
- `[Button("name")]` for multiple POST actions on the same route
- Anti-forgery tokens on POST forms (CSRF protection)
- No secrets, connection strings, or sensitive data in controller code

## Patterns
- `SessionUser` for current user context (not `HttpContext.User` directly)
- `SetErrorMessage()`, `SetInfoMessage()` for flash messages (not `TempData` directly)
- Property injection for `ServicesWrapper`: `public ServicesWrapper ServicesWrapper { get; set; }`

## OWASP Compliance
- Input validation on all parameters
- No raw SQL or string concatenation in queries
- Proper encoding of user-supplied output (XSS prevention)
- Authorization checks before data access

Flag any violations and suggest fixes with code examples.
