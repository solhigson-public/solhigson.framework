# Solhigson Framework — Project Rules

## Project Info
- **Path**: `C:\Users\eawag\My Drive\Source\Solhigson\solhigson.framework.core`
- **Type**: .NET class library / NuGet packages (framework used by all Solhigson projects)
- **Repo**: https://github.com/solhigson-public/solhigson.framework
- **CI/CD**: Azure Pipelines (`azure-pipelines.yml`) — builds on `master`, packages to NuGet on master when `package=true`

## Overview
Solhigson Framework is the shared foundation library consumed by all Solhigson .NET projects. It provides base types for web controllers, services, identity/RBAC, EF Core caching, auditing, notifications, and a code generation tool for the repository pattern.

## Solution Structure (`src/Solhigson.Framework.sln`)

| Project | Type | NuGet Package | Description |
|---------|------|---------------|-------------|
| `Solhigson.Framework` | Library | `Solhigson.Framework.Core` | Main framework: web base controllers, services, identity, EF Core caching, auditing, logging, notifications, DI attributes |
| `Solhigson.Utilities` | Library | `Solhigson.Utilities` | Lightweight utilities: `ResponseInfo`/`ResponseInfo<T>` structs, crypto, HTTP helpers, string extensions, LINQ helpers, JWT |
| `Solhigson.Framework.EfCoreTool` | .NET Tool (CLI) | `Solhigson.Framework.EfCoreToolGen` | Code generator (`solhigson-ef`) that scaffolds repository pattern files from EF Core models |
| `Solhigson.Framework.Tests` | Test (xUnit) | — | Unit tests (in `Whiteboard/` solution folder) |
| `Solhigson.Framework.Playground` | Console | — | Playground/scratch project (in `Whiteboard/` solution folder) |
| `Solhigson.Framework.Benchmarks` | Console | — | BenchmarkDotNet performance tests (in `Whiteboard/` solution folder) |

## Target Framework
- .NET 10 (`net10.0`), C# 14, nullable enabled
- `PackageVersion` in csproj controls NuGet version (currently `10.0.16` for Framework, `10.0.1` for Utilities/EfCoreTool)

## Key Modules in `Solhigson.Framework`

| Directory | Purpose |
|-----------|---------|
| `Web/` | `SolhigsonMvcControllerBase`, `SolhigsonApiControllerBase`, attributes (`PermissionAttribute`, `ButtonAttribute`), Swagger, middleware |
| `Identity/` | `SolhigsonIdentityManager`, `SolhigsonIdentityDbContext`, `SolhigsonPermission`, `SolhigsonRoleGroup`, `PermissionManager`, RBAC |
| `Services/` | `ServiceBase` (partial, code-generated + custom), `NotificationService`, `SolhigsonConfigurationService` |
| `Data/` | `PagedList<T>`, `ScriptsManager`, caching (`CacheManager`, `TableChangeTracker`), `IDateSearchable` |
| `EfCore/` | EF Core caching interceptor (`EfCoreCachingSaveChangesInterceptor`), `EntityChangeTracker` |
| `Persistence/` | Framework's own entities (`AppSetting`, `NotificationTemplate`), repositories, cache models |
| `Infrastructure/` | Constants, DI attributes (`DependencyInjectAttribute`, `DependencyLifetime`), dependency registration |
| `Auditing/` | `AuditHelper`, `AuditInfo` (uses Audit.NET) |
| `Logging/` | NLog targets (Hangfire, xUnit test output helper) |
| `Notification/` | `IMailProvider`, `ISmsProvider`, `SolhigsonSmtpMailProvider`, `AttachmentHelper` |
| `Extensions/` | `LoggerExtensions` (`this.LogError(e)` pattern) |
| `Utilities/` | Internal helpers |
| `Dto/` | Framework DTOs: `AppSettingDto`, `NotificationTemplateDto`, `SolhigsonPermissionDto` |
| `Mocks/` | `MockHangfireBackgroundJobClient` for testing |

## Key Types in `Solhigson.Utilities`

| Type | Location | Purpose |
|------|----------|---------|
| `ResponseInfo` (struct) | `Dto/ResponseInfo.cs` | Standard response wrapper — `IsSuccessful`, `StatusCode`, `Message`. Static factories: `SuccessResult()`, `FailedResult()` |
| `ResponseInfo<T>` (struct) | `Dto/ResponseInfo.cs` | Generic variant with `Data` property. Composes `ResponseInfo` internally (not inheritance) |
| `CryptoHelper` | `Security/` | Encryption/decryption utilities |
| `HttpUtils` | Root | HTTP helper methods |
| `HelperFunctions` | Root | General utility methods |
| `EnumUtil` | Root | Enum parsing utilities |
| `StatusCode` | Root | Status code constants (`Successful`, `UnExpectedError`, etc.) |

## EfCoreTool Code Generation
- CLI tool: `solhigson-ef`
- Generates `.generated.cs` files from EF Core entity models using embedded template files in `Templates/`
- Generated artifacts: repository interfaces, repository base classes, cached repository bases, DTOs, cache models, service base, wrapper interfaces
- Template naming convention: `Placeholder` → replaced with actual entity name during generation

## Versioning & Publishing
- Version bumps: update `<PackageVersion>` in the relevant `.csproj`
- Azure Pipelines auto-publishes to NuGet.org on `master` when pipeline variables are set
- Three independent packages: `Solhigson.Framework.Core`, `Solhigson.Utilities`, `Solhigson.Framework.EfCoreToolGen`

### Pipeline Push Flags (`azure-pipelines.yml`)
Three boolean variables control which NuGet packages get pushed on a master build:
- `packageFramework` — gates the `nuget_package_framework` stage (packs/pushes `Solhigson.Framework.Core`)
- `packageTool` — gates the `nuget_package_efCoreTool` stage (packs/pushes `Solhigson.Framework.EfCoreToolGen`)
- `packageUtilities` — gates the `nuget_package_utilities` stage (packs/pushes `Solhigson.Utilities`)

Each stage builds the full solution (so build errors in any project fail the stage), but only packs and pushes the specific project for that stage.

**Rules:**
- Only enable the flag for the package being released — disable others to avoid failed pushes on unchanged versions
- After a package is successfully pushed, disable its flag until the next release
- When packages have cross-references (e.g., Framework depends on Utilities), push the dependency first, wait for NuGet availability, then push the dependent package in a separate commit

## Important Rules
- **Never modify `.generated.cs` files** — place custom code in the corresponding partial class
- **`ResponseInfo`/`ResponseInfo<T>` are structs** — they are value types, not reference types
- **This is a library project** — changes here affect all consuming projects. Test thoroughly and bump `PackageVersion` on every release
- **Whiteboard projects** (Playground, Tests, Benchmarks) are for development/testing only — not packaged
