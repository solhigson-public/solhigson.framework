---
description: Add a new ASP.NET Core endpoint following project conventions.
---

Add a new endpoint to this project. Follow these steps in order:

## 1. Determine Endpoint Type
- **MVC** (returns Razor views) — goes in `Controllers/Mvc/`, inherits `MvcBaseController`
- **API** (returns JSON) — goes in `Controllers/Api/`, inherits `ApiBaseController`
- Ask which type if unclear from context.

## 2. Identify or Create Controller
- Check if an appropriate controller already exists for this resource/feature.
- If creating new: follow naming `{Resource}Controller.cs`, correct folder, correct base class.

## 3. Define DTOs
- Create `record` types for request/response DTOs in `Domain/ViewModels/` or appropriate DTO namespace.
- Name: `{Action}{Resource}ViewModel` for MVC, `{Resource}Dto` for API.
- Use `required` properties, data annotations for validation (`[Required]`, `[StringLength]`).
- Never abbreviate ViewModel as Vm.

## 4. Add Service Method
- Add method to appropriate service (inherits `ServiceBase`).
- Return `ResponseInfo<T>` — follow the standard pattern:
  ```csharp
  var response = new ResponseInfo<T>();
  try
  {
      // logic
      return response.Success(data);
  }
  catch (Exception e)
  {
      this.LogError(e);
  }
  return response.Fail();
  ```
- Use Mapster for mapping: `entity.Adapt<Dto>()`.
- Access DB via `RepositoryWrapper.DbContext`.

## 5. Add Repository Interaction (if needed)
- Use existing repository methods where possible.
- For new queries: add to the appropriate repository interface and implementation.
- Use `AsNoTracking()` for read-only queries.
- Never edit `.generated.cs` files — use partial classes.

## 6. Wire Up Controller Action
- Add route attribute: `[HttpGet("...")]` or `[HttpPost("...")]`.
- Add auth attribute: `[AllowAnonymous]` or `[Permission("...")]`.
- Call service via `ServicesWrapper`, return appropriate result.

## 7. Add Razor View (MVC only)
- Create view in matching `Views/{Controller}/` folder.
- Follow SEO conventions: `ViewBag.Title`, `ViewBag.MetaDescription`, `ViewBag.Robots`.
- Semantic HTML5 structure.

## 8. Suggest Tests
- List which unit tests should be added for the new service method.
- List integration test scenarios for the endpoint.
