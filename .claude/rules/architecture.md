Clean Architecture layer dependency rules — enforce strictly in all projects:

**Layer Dependencies (inner layers never reference outer):**
- **Domain**: Zero dependencies on other layers. No EF Core, no Infrastructure types.
- **Application**: Depends only on Domain. Defines service interfaces, DTOs, business logic.
- **Infrastructure**: Depends on Application + Domain. Implements repositories, DbContext, external integrations.
- **Web.Ui / API**: Depends on Application (and Domain for shared types). Never imports Infrastructure types directly for business logic.

**What lives where:**
- Domain: Entities (`record` inheriting `EntityBase`), value objects, enums, domain events
- Application: Services (`ServiceBase`), DTOs, ViewModels, `ServicesWrapper`, `IRepositoryWrapper` interface
- Infrastructure: Repository implementations, `AppDbContext`, `ReadOnlyAppDbContext`, EF configurations, `.generated.cs` files
- Web.Ui: Controllers (`MvcBaseController` / `ApiBaseController`), Razor views, static assets, Startup/Program

**Naming:**
- `{AppName}.Domain`, `{AppName}.Application`, `{AppName}.Infrastructure`, `{AppName}.Web.Ui`
- `{AppName}.Web.Hangfire` for background job dashboard
- `{AppName}.AppHost` for .NET Aspire orchestration
- `{AppName}.ServiceDefaults` for shared Aspire config

**DI Registration:**
- Autofac modules in Infrastructure register implementations against Application interfaces
- `[DependencyInject]` attribute for auto-registration
- Never resolve Infrastructure types directly from controllers
