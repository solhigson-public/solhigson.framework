# Service & Facade Patterns

## Contents

- [Service Layer Patterns](#service-layer-patterns)
- [Facade Service Pattern](#facade-service-pattern)

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
5. **Error isolation** — facades handle partial failures (e.g. home page: events load but tours fail → return what succeeded).

### Naming
`{Audience}FacadeService` — one per audience:
- `PublicFacadeService` — anonymous public pages
- `CreatorFacadeService` — creator dashboard
- `VendorFacadeService` — vendor dashboard
- `AdminFacadeService` — admin dashboard

### When to Use
- Controller action needs **2+ domain service calls** → use facade
- Controller action needs **1 domain service call** → call domain service directly

### Example
```csharp
public partial class PublicFacadeService(IRepositoryWrapper repositoryWrapper) : ServiceBase(repositoryWrapper)
{
    public async Task<ResponseInfo<EventDetailData>> GetEventDetailAsync(string slug)
    {
        var response = new ResponseInfo<EventDetailData>();
        try
        {
            var eventResponse = await ServicesWrapper.EventService.GetEventBySlugAsync<EventDto>(slug);
            if (!eventResponse.IsSuccessful || eventResponse.Data is null)
                return response.Fail(eventResponse.Message);

            var ev = eventResponse.Data;

            var highlightsTask = ServicesWrapper.EventService.GetHighlightsAsync<EventHighlightDto>(ev.Id);
            var performersTask = ServicesWrapper.EventService.GetPerformersAsync<PerformerDto>(ev.Id);
            var ticketsTask = ServicesWrapper.EventService.GetTicketTypesAsync<TicketTypeDto>(ev.Id);
            var organizerTask = GetOrganizerInfoAsync(ev.CreatorId);

            await Task.WhenAll(highlightsTask, performersTask, ticketsTask, organizerTask);

            return response.Success(new EventDetailData(ev, highlightsTask.Result, ...));
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