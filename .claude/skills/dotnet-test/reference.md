## Test Infrastructure Reference

### Project Structure

```
Tests/
├── TestBase.cs                  (thin facade, delegates to fixtures)
├── Fixtures/
│   ├── TestContainerBuilder.cs  (DI + DB setup, returns IServiceProvider)
│   ├── TestDataSeeder.cs        (seed data creation, single Seed() method)
│   ├── <Domain>Fixtures.cs      (domain-specific lookup/creation helpers)
│   ├── <Feature>Fixtures.cs     (feature-specific orchestration/verification helpers)
│   ├── RequestBuilder.cs        (request construction/encryption/sending)
│   └── ControllerTestFixture.cs (controller instantiation with HTTP context)
├── Mocks/                       (mock/stub implementations of external dependencies)
├── Dto/                         (test-specific DTOs and records)
├── Extensions/
│   └── AssertionExtensions.cs   (Shouldly extension methods)
└── Tests/
    ├── <Feature>TestBase.cs     (optional: thin facade composing feature fixtures)
    └── <Feature>Tests.cs        (test classes inheriting TestBase or <Feature>TestBase)
```

### TestBase Decomposition Rules

1. **TestBase MUST be a thin facade** — MUST compose fixture classes via properties, MUST expose `protected` one-liner delegates so existing test classes don't need changes:
   ```csharp
   protected {Entity}Dto GetDefault{Entity}() => {Domain}Fixtures.GetDefault{Entity}();
   protected TK SendRequest<T, TK>(...) => RequestBuilder.SendRequest<T, TK>(...);
   ```

2. **TestContainerBuilder** — MUST own DI bootstrapping and DB lifecycle. Constructor MUST build everything, returns `(IServiceProvider, DbConnection?)` tuple. MUST NOT contain test logic.

3. **TestDataSeeder** — MUST own all seed data creation. MUST take `IServiceProvider`, `IRepositoryWrapper`, `ServicesWrapper`, and domain fixtures via primary constructor. MUST have single public method: `void Seed()`.

4. **Domain Fixtures** — MUST group related helpers by domain concept (e.g. `UserFixtures` for user CRUD/lookup). MUST use primary constructor injection with `IRepositoryWrapper` and `ServicesWrapper`.

5. **Feature Fixtures** — when a group of tests needs shared domain setup beyond what TestBase provides, MUST extract a `<Feature>Fixtures` class. It MUST take `TestBase` as a constructor parameter (using `protected internal` visibility on needed members) and MUST own all feature-specific logic: entity creation, orchestration, verification.

6. **Feature Test Bases** — optional thin facades that inherit `TestBase`, compose a `<Feature>Fixtures` instance, and expose `protected` one-liner delegates.

7. **Request builders** — MUST isolate request construction, encryption, and sending. MUST take `IServiceProvider`, `ServicesWrapper`, `IRepositoryWrapper`, and domain fixtures.

8. **Assertion extensions** — MUST use Shouldly extension methods (e.g. `ShouldBeSuccessful()` on `ResponseInfo`) instead of helper methods on base classes.

### When to Decompose

- Any test base class exceeding ~150 lines
- When test base handles 3+ unrelated responsibilities
- When adding new test infrastructure that doesn't fit cleanly into the existing base

---

### Test Container (TestContainerBuilder)

MUST use a **full Autofac container** that mirrors production DI:

```csharp
var builder = new ContainerBuilder();
var services = new ServiceCollection();
var configuration = new ConfigurationBuilder().Build();

// Real DI registration
services.AddLogging();
services.AddSolhigsonIdentityManager<AppUser, AppDbContext>(option => { ... });
services.AddMemoryCache();
builder.RegisterSolhigsonDependencies(configuration, connectionString);
builder.RegisterModule(new AutofacModule(configuration, new MockHostEnvironment()));
builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

// Auto-register test mocks from test assembly
builder.RegisterIndicatedDependencies(Assembly.GetAssembly(typeof(TestBase)));

builder.Populate(services);
var serviceProvider = new AutofacServiceProvider(builder.Build());
```

Autofac handles circular dependencies (ServicesWrapper <-> services) via property injection automatically.

### Database: SQLite In-Memory

MUST use **SQLite in-memory** (NEVER EF Core InMemory provider) for real SQL execution:

```csharp
_connection = new SqliteConnection("Filename=:memory:");
_connection.Open();

builder.Register(_ => {
    var opt = new DbContextOptionsBuilder<AppDbContext>();
    opt.UseSqlite(_connection);
    opt.ConfigureWarnings(x => x.Ignore(RelationalEventId.AmbientTransactionWarning));
    return new AppDbContext(opt.Options);
}).AsSelf().InstancePerDependency();
```

Schema creation via `GenerateCreateScript()` with SQLite compatibility fix:
```csharp
var script = dbContext.Database.GenerateCreateScript()
    .Replace("(MAX)", "(8000)", StringComparison.OrdinalIgnoreCase);
dbContext.Database.ExecuteSqlRaw(script);
```

Each xUnit test method gets its own class instance -> own TestContainerBuilder -> own SQLite connection -> fully isolated.

### Mocking Strategy

#### Hand-Written Mocks (preferred for most cases)

MUST use when the mock needs:
- **Cross-call state** — e.g., storage service storing/retrieving files in a `Dictionary<string, byte[]>`
- **Real execution logic** — e.g., Hangfire client executing jobs immediately instead of queuing
- **Concrete class extension** — e.g., overriding specific methods on a production service
- **DI auto-registration** — all hand-written mocks use `[DependencyInject]` attribute

```csharp
[DependencyInject(RegisteredTypes = [typeof(IStorageService)])]
public class MockStorageService : IStorageService { ... }
```

`RegisterIndicatedDependencies(testAssembly)` auto-registers these, overriding production types.

#### NSubstitute Stubs (for trivial interfaces only)

MUST use only for **stateless, no-op interfaces** where no behavior is needed:
```csharp
builder.RegisterInstance(Substitute.For<IAppLogService>());
builder.RegisterInstance(Substitute.For<IAuditLogService>());
```

#### Standard Mocks (create in every project)

| Mock Class | Implements | Behavior |
|-----------|-----------|----------|
| `MockHostEnvironment` | `IWebHostEnvironment` | Returns test paths, `EnvironmentName = "UnitTest"` |
| `MockStorageService` | `IStorageService` | In-memory `Dictionary<string, byte[]>` |
| `MockRedisCacheService` | `IRedisCacheService` | In-memory `ConcurrentDictionary<string, string>` with JSON serialize/deserialize |
| `MockHangfireBackgroundJobClient` | `IBackgroundJobClient` | No-op or immediate execution |
| `MockNotificationService` | `INotificationService` | No-op (SMS/email) |

Mock files live in `Tests/Mocks/` folder.

### Assertion Conventions

**MUST NEVER use `Assert.*`** — MUST use Shouldly exclusively:
- `ShouldBe()`, `ShouldNotBeNull()`, `ShouldBeTrue()`
- `ShouldContainKey()`, `ShouldBeOfType<T>()`
- `ShouldBeEquivalentTo()`
- Custom: `ShouldBeSuccessful()` on `ResponseInfo`

### NuGet Packages

Required for test projects:
- `xunit.v3` + `xunit.runner.visualstudio` — test framework
- `Shouldly` — assertions (NEVER `Assert.*`)
- `NSubstitute` — MUST use for trivial interface stubs only
- `Microsoft.EntityFrameworkCore.Sqlite` — SQLite provider (version matches main EF Core)
- `Microsoft.NET.Test.Sdk` — test execution
- `coverlet.collector` — code coverage
