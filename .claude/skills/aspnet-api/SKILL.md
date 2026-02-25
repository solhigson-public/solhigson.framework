---
name: aspnet-api-developer
description: Design, implement, and test ASP.NET Core REST APIs and MVC endpoints following Clean Architecture and project conventions.
---

# ASP.NET Core API Developer Skill

When working with ASP.NET Core controllers and endpoints:

## Architecture
- Follow Controller -> Service -> Repository layering strictly
- Controllers are thin — no business logic, only orchestration
- Services own all business logic and return `ResponseInfo<T>`
- Repositories handle data access via EF Core

## Controller Conventions
- MVC controllers inherit `MvcBaseController`, live in `Controllers/Mvc/`
- API controllers inherit `ApiBaseController`, live in `Controllers/Api/`
- Route attributes on every action: `[HttpGet("...")]`, `[HttpPost("...")]`
- Auth on every action: `[AllowAnonymous]` or `[Permission("...")]`
- Access services via `ServicesWrapper` property injection

## Service Pattern
- All services inherit `ServiceBase`
- Every method returns `ResponseInfo` or `ResponseInfo<T>`
- Standard try/catch with `this.LogError(e)` and `return response.Fail()`
- Use Mapster for mapping: `entity.Adapt<Dto>()`, never AutoMapper

## DTOs & ViewModels
- `record` types for all DTOs and ViewModels
- Name: `{Entity}Dto` for API, `{Action}{Entity}ViewModel` for MVC
- Never abbreviate ViewModel as Vm
- Data annotations for validation: `[Required]`, `[StringLength]`

## Security (OWASP)
- Input validation on all endpoints
- Anti-forgery tokens on POST forms
- Proper authorization checks before data access
- No secrets in code, no raw SQL concatenation
- Encode user-supplied output to prevent XSS

## Testing
- Suggest unit tests for new service methods
- Mock `IRepositoryWrapper` in tests
- Test both success and failure ResponseInfo paths
