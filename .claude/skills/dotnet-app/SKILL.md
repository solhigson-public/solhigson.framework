---
name: dotnet-app
description: Application and web layer patterns for ASP.NET Core — services, facades, controllers, ViewModels, permissions, error handling.
---

# .NET Application Layer Skill

Covers everything above the data layer: services, facades, controllers, ViewModels, permissions, and error handling. For data layer (entities, queries, repositories, archive), MUST use the `efcore` skill instead.

## When This Skill Is Invoked

- Writing or modifying service methods
- Writing or modifying controller actions
- Implementing facade services
- Mapping DTOs to ViewModels
- Adding permissions to controller actions
- Implementing error handling on public pages

## Architecture

```
Client -> Controller -> [FacadeService] -> DomainService -> RepositoryWrapper -> DbContext
```

- Controllers are thin — no business logic, only orchestration
- Services own all business logic and return `ResponseInfo<T>`
- Facades compose multiple domain services for controllers needing 2+ service calls
- ViewModels are presentation-only — mapped in Web.Ui layer via static mapper classes

## Key Conventions

- All services inherit `ServiceBase` (partial class)
- Every service method returns `ResponseInfo` or `ResponseInfo<T>`
- Mapster for all mapping: `entity.Adapt<Dto>()`, `request.Adapt(entity)`, `ProjectToType<TDto>()`
- `ServicesWrapper` for service access, `RepositoryWrapper` for data access
- Property injection for `ServicesWrapper` on controllers and services
- Primary constructor injection for non-circular dependencies

## Reference Files

MUST read the reference file matching the current task:

- **`references/services.md`** — service layer patterns (declaration, ResponseInfo, entity CRUD, read patterns) and facade service patterns (naming, parallel execution, error isolation)
- **`references/controllers.md`** — ViewModel mapper pattern, public page error handling, permissions & RBAC (attributes, enforcement, antiforgery), and controller templates
- **`references/patterns.md`** — idempotency patterns, state machine patterns, resilience patterns (Polly v8), .NET conventions & style, and related commands
