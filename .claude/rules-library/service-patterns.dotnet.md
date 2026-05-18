---
name: Service Layer Patterns (.NET)
description: "Service layer conventions — ServiceBase inheritance, ResponseInfo<T> return types, record request DTOs with init/required, CRUD patterns and /add-endpoint command"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
---

# Service Layer Patterns

All services, including read-only query services, MUST inherit `ServiceBase` and return `ResponseInfo<T>` where T is the result (including query results). Request DTOs MUST use `record` types with `init` setters and `required` on mandatory fields.

## Architecture

```
Client -> Controller -> [FacadeService] -> DomainService -> RepositoryWrapper -> DbContext
```

- Controllers are thin — no business logic, only orchestration
- Services own all business logic and return `ResponseInfo<T>`
- Facades compose multiple domain services for controllers needing 2+ service calls
- ViewModels are presentation-only — mapped in Web.Ui layer via static mapper classes

---

## Service Layer Patterns

### Service Declaration
```csharp
public partial class {Feature}Service(IRepositoryWrapper repositoryWrapper) : ServiceBase(repositoryWrapper)
```

### ResponseInfo Pattern
```csharp
public async Task<ResponseInfo<T>> DoSomethingAsync(...)
{
    var response = new ResponseInfo<T>();
    try
    {
        // ... logic ...
        return response.Success(data);
    }
    catch (Exception e)
    {
        this.LogError(e);
    }
    return response.Fail();
}
```
For simple success/fail without data: `ResponseInfo.SuccessResult()` / `ResponseInfo.FailedResult("message")`.

### Entity Creation (repo + Mapster)
```csharp
var entity = RepositoryWrapper.{Entity}Repository.New();
request.Adapt(entity);
entity.CreatorId = creatorId;       // non-mapped fields
await RepositoryWrapper.SaveChangesAsync();
return response.Success(entity.Adapt<{Entity}Dto>());
```

### Entity Update (repo + Mapster)
```csharp
var entity = await RepositoryWrapper.{Entity}Repository.GetByIdAsync(id);
// validate ownership
request.Adapt(entity);
await RepositoryWrapper.SaveChangesAsync();
return response.Success(entity.Adapt<{Entity}Dto>());
```

### Read Patterns
- **By ID (archive-aware)**: `ArchiveService.GetByIdAsync<TDto, TActive, TArchive>(id)`
- **By slug (archive fallback)**: `Repository.GetBySlugAsync<TDto>(slug)` then fallback to `DbContext.Set<TArchive>()`
- **List (active only, paged)**: `DbContext.Set<T>().AsNoTracking().Where(...).ProjectToType<TDto>().ToPagedListAsync()`
- **Children (archive-aware)**: `ArchiveService.GetQueryAsync<TDto, TActive, TArchive, TBase>(predicate, checkArchive)`

### Child Entity Pattern
Children MUST ALWAYS validate ownership via parent:
```csharp
var child = await RepositoryWrapper.{Child}Repository.GetByIdAsync(childId);
var parent = await RepositoryWrapper.{Parent}Repository.GetByIdAsync(child.{Parent}Id);
if (parent is null || parent.CreatorId != creatorId)
    return response.Fail("Not authorized...");
```

### Cascade Delete (FK order)
```csharp
await DbContext.Set<JunctionEntity>().Where(...).ExecuteDeleteAsync();
await DbContext.Set<ChildEntity>().Where(x => x.ParentId == id).ExecuteDeleteAsync();
DbContext.Set<ParentEntity>().Remove(parent);
```

### Slug Generation (GeneratedRegex)
```csharp
private string GenerateSlug(string title)
{
    var slug = SlugRegex().Replace(title.ToLowerInvariant(), "-").Trim('-');
    return $"{slug}-{Guid.NewGuid():N}"[..Math.Min(slug.Length + 33, 500)];
}

[GeneratedRegex(@"[^a-z0-9]+")]
private static partial Regex SlugRegex();
```

### Repository Methods Available (per generated interface)
- `GetByIdAsync(id)` — tracked entity for writes
- `GetByIdAsync<TDto>(id)` — projected, AsNoTracking
- `GetByCreatorIdAsync(creatorId)` — on entities with CreatorId index
- `GetBySlugAsync(slug)` / `GetBySlugAsync<TDto>(slug)` — on entities with Slug index
- `GetBy{IndexedProperty}Async(value)` — per entity index
- Composite unique index: `GetBy{Prop1}And{Prop2}Async(a, b)`
- `New()` — creates entity with sequential GUID, adds to change tracker

### ArchiveService Signatures
```csharp
GetByIdAsync<TResult, TActive, TArchive>(id, readFromPrimary?)
GetQueryAsync<TResult, TActive, TArchive, TBase>(predicate, checkArchive?, request?, readFromPrimary?, pageSize?)
```

### Request DTO Pattern
- `record` types with `init` setters, `required` on mandatory fields
- Same property names as entity (enables Mapster auto-mapping)
- Create requests may include optional child collections
- Update requests omit child collections (children managed via separate endpoints)
- StringLength/Required annotations mirror entity constraints

---

## Facade Service Pattern

Facade services orchestrate multiple domain services into composite responses.

### Rules
1. **1:1 controller-to-facade** — each controller MUST call exactly one facade. MUST NOT have cross-facade orchestration in controllers.
2. **No facade-to-facade calls** — facades MUST call domain services only, MUST NEVER call other facades.
3. **Composite data records, not ViewModels** — facades MUST return data records (e.g. `EventDetailData`). MUST NOT return ViewModels. ViewModel mapping MUST stay in Web.Ui.
4. **Parallel execution** — facades own `Task.WhenAll` composition for independent sub-calls.
5. **Error isolation** — facades handle partial failures (e.g. home page: events load but tours fail -> return what succeeded).

### Naming
`{Audience}FacadeService` — one per audience:
- `PublicFacadeService` — anonymous public pages
- `CreatorFacadeService` — creator dashboard
- `VendorFacadeService` — vendor dashboard
- `AdminFacadeService` — admin dashboard

### When to Use
- Controller action needs **2+ domain service calls** -> use facade
- Controller action needs **1 domain service call** -> call domain service directly

### Base Class
Facades MUST inherit `FacadeServiceBase` (NOT `ServiceBase`). Facades are pure orchestrators — no direct repository access. `FacadeServiceBase` provides:
- `ServicesWrapper` for domain service access
- `WhenAllAsync` overloads (arity 2-9) with structured cancellation via linked `CancellationTokenSource`
- Logging via `this.LogError(e)`

Facades MUST live in `src/<ProjectName>.Application/Facades/` (NOT in `Services/`).

### Example
```csharp
public partial class PublicFacadeService : FacadeServiceBase
{
    public async Task<ResponseInfo<EventDetailData>> GetEventDetailAsync(
        string slug, CancellationToken cancellationToken = default)
    {
        var response = new ResponseInfo<EventDetailData>();
        try
        {
            var eventResponse = await ServicesWrapper.EventService
                .GetEventBySlugAsync<EventDto>(slug, cancellationToken: cancellationToken);
            if (!eventResponse.IsSuccessful || eventResponse.Data is null)
                return response.Fail(eventResponse.Message);

            var ev = eventResponse.Data;

            var (highlights, performers, tickets, organizer) = await WhenAllAsync(
                ct => ServicesWrapper.EventService.GetHighlightsAsync<EventHighlightDto>(ev.Id, cancellationToken: ct),
                ct => ServicesWrapper.EventService.GetPerformersAsync<PerformerDto>(ev.Id, cancellationToken: ct),
                ct => ServicesWrapper.EventService.GetTicketTypesAsync<TicketTypeDto>(ev.Id, cancellationToken: ct),
                ct => GetOrganizerInfoAsync(ev.CreatorId, ct),
                cancellationToken);

            return response.Success(new EventDetailData(ev, highlights.Data, ...));
        }
        catch (Exception e)
        {
            this.LogError(e);
        }
        return response.Fail();
    }
}
```

---

## ViewModel Mapper Pattern

For directives (static class, placement, per-area mapping, thin controller), MUST comply with `viewmodel-mapper-pattern.dotnet.md`.

### Example
```csharp
public static class PublicViewModelMapper
{
    public static EventBrowseCardViewModel ToEventBrowseCard(EventDto dto, string currencySymbol)
    {
        return new EventBrowseCardViewModel(
            Slug: dto.Slug, Title: dto.Title, Category: dto.Category.ToString(),
            Price: FormatPrice(minPrice, currencySymbol)
        );
    }

    public static string FormatPrice(decimal amount, string currencySymbol)
        => $"{currencySymbol}{amount:N0}";

    public static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : (parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : "?");
    }
}
```

---

## Public Page Error Handling

For directives (browse/list, detail, aggregation, checkout error strategies), MUST comply with `public-page-error-handling.dotnet.md`.

### Browse / List Pages
On service error: set flash message + render page with empty results.
```csharp
var response = await ServicesWrapper.SomeService.GetItemsAsync(page, pageSize);
if (!response.IsSuccessful)
{
    SetErrorMessage("Unable to load items. Please try again.");
}
var items = response.IsSuccessful ? response.Data : new PagedList<SomeDto>();
```
The view's empty-state UI handles "no results." The flash message distinguishes "error" from "nothing matches."

### Detail Pages
Two distinct failure modes — MUST NEVER conflate them:
```csharp
var response = await ServicesWrapper.SomeService.GetBySlugAsync(slug);
if (!response.IsSuccessful)
{
    SetErrorMessage("Unable to load this item. Please try again.");
    return RedirectToAction("Browse");
}
if (response.Data is null)
{
    return NotFound();
}
```
MUST NEVER return 404 for transient errors — the item may exist but the lookup failed.

### Aggregation Pages (Home / Dashboard)
On partial failure: render what succeeded, skip failed sections. No flash messages — empty sections are acceptable on discovery surfaces.

### Checkout / Transactional Pages
- No active order -> redirect to home with `SetInfoMessage`
- Order fetch fails -> redirect to home with `SetErrorMessage`
- MUST NEVER show a broken checkout page

---

## Permissions & RBAC

### Permission Definition
All permissions in `Permission.cs` static partial class with `PermissionInfo*` attributes:
- `PermissionInfoMenuRoot` — top-level menu group
- `PermissionInfoChildMenu` — visible menu item under a root
- `PermissionInfoChildNonMenu` — action-only permission, no menu entry

Naming: `permission.{area}.{action}` (lowercase, dot-separated).

Source generator converts Description to PascalCase -> `Permission.Users = "permission.users.view"`.

### Controller Auth — Mandatory
Every action MUST have one of:
- `[AllowAnonymous]` — explicitly public
- `[Permission(Permission.X)]` — requires specific RBAC permission
- `[Authorize]` — requires authentication only, no role restriction

**Least privilege:** When a matching `Permission.*` constant exists for the action's business domain, MUST use `[Permission(Permission.X)]` instead of `[Authorize]`. `[Authorize]` is appropriate only when the action genuinely applies to ALL authenticated users regardless of role (e.g., toggling a personal favorite). MUST NOT use `[Authorize]` as a convenience shortcut when a scoped permission would be more precise.

**MUST NOT have naked actions.** An action without any of these three attributes is a security bug.

MUST NEVER hardcode permission strings — MUST ALWAYS use generated `Permission.*` constants.

### Antiforgery

- MUST register `AutoValidateAntiforgeryTokenAttribute` as a global MVC filter
- MUST NOT use `[ValidateAntiForgeryToken]` on individual actions — global filter handles it
- Actions that receive external callbacks (webhooks, payment notifications) MUST use `[IgnoreAntiforgeryToken]`
- Token delivery for forms: automatic via `@Html.AntiForgeryToken()` hidden input
- Token delivery for AJAX: MUST follow `ajax-pattern.dotnet.md` CSRF section

### Service Call Counting

- MUST NOT call more than one service from a controller action. If a facade is used, all service calls MUST go through the facade.
- Any call to any service counts, including cached/utility services. Cached or fast services are NOT exempt.
- If a controller action calls a facade AND another service, that is 2 calls — MUST move the second call into the facade.

---

## Controller Templates

### MVC Controller
```csharp
[Route("orders")]
public class OrdersController : MvcBaseController
{
    public ServicesWrapper ServicesWrapper { get; set; }

    [HttpGet("")]
    [Permission(Permission.Orders)]
    public async Task<IActionResult> Index()
    {
        var result = await ServicesWrapper.OrderService.GetListAsync();
        return View(result.Data);
    }
}
```

### API Controller
```csharp
[Route("api/[controller]")]
public class OrdersController : ApiBaseController
{
    public ServicesWrapper ServicesWrapper { get; set; }

    [HttpGet("{id}")]
    [Permission(Permission.Orders)]
    public async Task<IActionResult> Get(string id)
    {
        var result = await ServicesWrapper.OrderService.GetByIdAsync(id);
        return ApiResponse(result);
    }
}
```

### Auth Attributes
- `[AllowAnonymous]` — public endpoint
- `[Permission(Permission.X)]` — RBAC permission check
- `[Authorize]` — authentication only
- `[ThrottleByUser]` / `[ThrottleByParam]` — rate limiting
- `[Button("action")]` — disambiguate multiple POSTs on same route

### Flash Messages
```csharp
SetInfoMessage("Operation successful");
SetErrorMessage("Something went wrong");
```

### SEO (MVC Views)
```csharp
ViewBag.Title = "Page Title | AppName";
ViewBag.MetaDescription = "Description for search engines";
ViewBag.Robots = "index, follow";  // or "noindex, nofollow" for auth pages
```
