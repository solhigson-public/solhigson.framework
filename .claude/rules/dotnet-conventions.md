# .NET / C# Conventions

## Tech Stack
- **Backend**: ASP.NET Core / C# (.NET 9+, slnx format)
- **Frontend (Web)**: Razor views, jQuery/JavaScript, HTML/CSS
- **Cloud**: Azure (App Service, Azure Storage, Redis Cache)
- **CI/CD**: GitHub Actions, Azure Pipelines
- **Background Jobs**: Hangfire
- **Orchestration**: .NET Aspire (AppHost)

## Architecture & Patterns
- **Clean Architecture / DDD** — Domain, Application, Infrastructure, Web layers
- Project naming: `{AppName}.Domain`, `{AppName}.Application`, `{AppName}.Infrastructure`, `{AppName}.Web.Ui`
- Separate `{AppName}.Web.Hangfire` for background job dashboard
- `{AppName}.AppHost` for .NET Aspire orchestration
- `{AppName}.ServiceDefaults` for shared Aspire config

## C# / ASP.NET Core Conventions

### Naming & Style
- File-scoped namespaces (`namespace X;` not `namespace X { }`)
- PascalCase for public members, `_camelCase` for private fields
- **Acronyms in PascalCase**: Treat all acronyms (even 2-letter) as words — capitalize only the first letter. Examples: `RealityTv` not `RealityTV`, `DjMusic` not `DJMusic`, `McHost` not `MCHost`, `HttpClient` not `HTTPClient`, `IoStream` not `IOStream`
- **Never** use `Vm` as shorthand for `ViewModel` — always write `ViewModel` in full (e.g., `CampaignViewModel` not `CampaignVm`)
- `record` types for entities, DTOs, and value objects (not classes)
- `EntityBase` record as base for all domain entities with `Id`, `Created`, `Updated`
- Sequential GUIDs via `MassTransit.NewId.NextSequentialGuid()` for entity IDs
- String IDs (`string Id`), not int/Guid — stored as `VARCHAR(450)`

### Dependency Injection
- **Autofac** for DI container, not built-in MS DI alone
- `[DependencyInject]` attribute (from Solhigson.Framework) to auto-register services
- **Property injection** via Autofac for circular dependencies (e.g. `ServicesWrapper` on `ServiceBase`)
- **Primary constructor injection** for non-circular dependencies (e.g. `ConfigService(IRepositoryWrapper, ...)`)
- `ServicesWrapper` is the central service aggregator — injected into controllers and services
- Controllers use property injection: `public ServicesWrapper ServicesWrapper { get; set; }`

### Controller Patterns
- **MVC controllers** inherit `MvcBaseController` (extends `SolhigsonMvcControllerBase`)
- **API controllers** inherit `ApiBaseController` (extends `SolhigsonApiControllerBase`)
- Controllers organised in `Controllers/Mvc/` and `Controllers/Api/` folders
- MVC controllers return `View()`, `RedirectToAction()`, or `Redirect()`
- Route attributes on actions: `[HttpGet("dashboard")]`, `[HttpPost("login")]`
- `[AllowAnonymous]` for public endpoints, `[Permission("...")]` for RBAC
- Custom throttling: `[ThrottleByParam]`, `[ThrottleByUser]`
- `[Button("name")]` attribute for multiple POST actions on same route
- `SessionUser` property available on base controller for current user context
- `SetErrorMessage()`, `SetInfoMessage()`, `SetMessage()` for flash messages

### Service Layer
- All services inherit `ServiceBase` (partial class, code-generated + custom)
- `ServiceBase` has `ServicesWrapper` and `RepositoryWrapper` properties
- Services access DB via `RepositoryWrapper.DbContext` (EF Core)
- Return `ResponseInfo` / `ResponseInfo<T>` from all service methods
- Pattern: `var response = new ResponseInfo<T>(); ... return response.Success(data);` or `response.Fail("message")`
- `this.LogError(e)` extension method for structured logging (NLog)
- Mapster for object mapping: `entity.Adapt<Dto>()`, `model.Adapt(entity)`
- Pagination via `ToPagedListAsync()` returning `PagedList<T>`
- Caching via `FromCacheListAsync()`, `FromCacheSingleAsync()` EF Core extensions

### Repository Pattern
- `IRepositoryWrapper` aggregates all repositories + `DbContext`
- Generated repository base classes: `{AppName}RepositoryBase<T>`, `{AppName}CachedRepositoryBase<T>`
- Repository interfaces in `Infrastructure/Repositories/Abstractions/`
- Code generation via `Solhigson.Framework.efcoretool` — `.generated.cs` files are NEVER edited manually

### Authentication
- Dual auth: cookie-based session for MVC web, JWT Bearer for mobile APIs
- `SolhigsonIdentityManager<AppUser, AppDbContext>` for Identity operations
- Session stored in Redis, JWT token generation/validation in `UserService`
- RSA encryption for password fields from client-side
- Permission-based RBAC with `SolhigsonPermission`, role groups (System/Organisation)

### Entity & DTO Patterns
- Entities are `record` types inheriting `EntityBase`
- Data annotations for validation: `[Required]`, `[StringLength]`, `[Column(TypeName = "VARCHAR")]`
- `[CachedProperty]` attribute on properties that participate in caching
- `[AuditIgnore]` on properties excluded from audit trail
- DTOs use `record` types, named `{Entity}Dto` (e.g. `CountryDto`, `OrganisationDto`)
- ViewModels in `Domain/ViewModels/` namespace
- `SessionUser` record for auth session data
- Audit trail via `AuditHelper.AuditAsync()` with `AuditEntry`/`AuditChange`

### Database
- EF Core with SQL Server
- Read/write split: `AppDbContext` (primary) + `ReadOnlyAppDbContext` (replica)
- SQL retry logic via `SqlConfigurableRetryFactory`
- LINQ query style with explicit joins (not navigation properties) for complex queries

### Testing
- **xUnit** for test framework
- **Shouldly** for assertions — use Shouldly exclusively, never `Assert.Equal()` / `Assert.True()` / etc.
- **NSubstitute** for mocking where needed

## Web (jQuery/JavaScript/HTML/CSS)
- Razor views with `ViewBag` for metadata (Title, Robots)
- SEO metadata pattern: `ViewBag.Title`, `ViewBag.MetaDescription`, `ViewBag.Robots`
- Email templates with `[[placeholder]]` syntax
- jQuery for client-side interactivity
- Semantic HTML5, CSS classes over inline styles

## Mandatory Design Principles
- **DRY**: Follow DRY principles rigorously. Extract shared fields/logic into base classes. Prefer C# inheritance for shared properties (no EF table inheritance — each entity maps to its own table).

## Mandatory Code Standards
1. **Performance**: All code must be performant and efficient (CPU and memory). Avoid unnecessary allocations, use async/await properly, cache where appropriate.
2. **Security**: All code must follow secure coding best practices. Input validation, parameterized queries, proper auth checks, no secrets in code.
3. **OWASP Compliance**: All web code must be OWASP compliant — protect against XSS, CSRF, injection, broken auth, security misconfiguration, etc.
4. **SEO Compliance**: All web-facing code must be fully SEO compliant — semantic HTML, proper meta tags, structured data, crawlable content, performance optimized.

### .NET Performance Techniques
- Minimize heap allocations in hot paths — use `stackalloc`, `Span<T>`, `ReadOnlySpan<char>`, `StringPool`
- `async/await` all the way — never `.Result` or `.Wait()` (except framework `.GetResult()`)
- Prefer `ReadOnlySpan<char>` over string concatenation for URL/path building
- Use `SemaphoreSlim` (async) over `lock` for thread synchronization
- Use `is null` / `is not null` pattern matching over `== null`
- Prefer `TryParse`/`TryGetValue` over parse-and-catch for expected failure paths

## General .NET Rules
- Always handle errors explicitly — `try/catch` with `this.LogError(e)`, return `response.Fail()`
- Use strong typing — avoid `object` unless necessary
- Prefer immutability — `record` types, `required` properties, `init` setters
- `async/await` consistently — never `.Result` or `.Wait()` (except `.GetResult()` from framework)
- Don't modify `.generated.cs` files — they're overwritten by tooling
