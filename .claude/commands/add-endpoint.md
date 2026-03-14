---
description: Add a new ASP.NET Core endpoint following project conventions.
---

Add a new endpoint to this project. MUST follow these steps in order.

## Governed By

- `dotnet-conventions.md` — naming, record types, file-scoped namespaces
- `service-patterns.md` — service method patterns, DTO conventions
- `permissions-pattern.md` — auth attribute enforcement
- `generated-files.md` — partial class rules
- `seo-strategy.md` — SEO conventions for MVC views

## 1. Determine Endpoint Type
- **MVC** (returns Razor views) — MUST go in `Controllers/Mvc/`, MUST inherit `MvcBaseController`
- **API** (returns JSON) — MUST go in `Controllers/Api/`, MUST inherit `ApiBaseController`
- MUST ask which type if unclear from context.

## 2. Identify or Create Controller
- MUST check if a controller already exists for this resource/feature area.
- If creating new: MUST follow naming `{Resource}Controller.cs`, correct folder, correct base class.

## 3. Define DTOs
- MUST create `record` types in `Domain/ViewModels/` (MVC) or the DTO namespace for the feature area.
- Name: `{Action}{Resource}ViewModel` for MVC, `{Resource}Dto` for API.
- MUST use `required` properties, data annotations for validation (`[Required]`, `[StringLength]`).
- MUST NOT abbreviate ViewModel as Vm.

## 4. Add Service Method
- MUST add method to the service owning this resource — MUST comply with `service-patterns.md` and MUST invoke the `dotnet-app` skill for full code templates.
- MUST return `ResponseInfo<T>`, MUST use Mapster for mapping, MUST access DB via `RepositoryWrapper.DbContext`.

## 5. Add Repository Interaction (if needed)
- MUST use existing repository methods where possible.
- For new queries: MUST add to the repository interface and implementation for this entity.
- MUST use `AsNoTracking()` for read-only queries.
- MUST NOT edit `.generated.cs` files — MUST use partial classes.

## 6. Wire Up Controller Action
- MUST add route attribute: `[HttpGet("...")]` or `[HttpPost("...")]`.
- MUST add auth attribute per `permissions-pattern.md`.
- MUST call service via `ServicesWrapper`, MUST return the result type matching the endpoint type.

## 7. Add Razor View (MVC only)
- MUST create view in matching `Views/{Controller}/` folder.
- MUST follow SEO conventions per `seo-strategy.md`: `ViewBag.Title`, `ViewBag.MetaDescription`, `ViewBag.Robots`.
- MUST use semantic HTML5 structure.

## 8. Suggest Tests
- MUST list which unit tests MUST be added for the new service method.
- MUST list integration test scenarios for the endpoint.
