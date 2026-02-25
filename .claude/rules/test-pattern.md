# Test Decomposition Pattern

When creating or refactoring test infrastructure, decompose monolithic test base classes into focused, composable fixtures.

## Structure

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

## Rules

1. **TestBase is a thin facade** — composes fixture classes via properties, exposes `protected` one-liner delegates so existing test classes don't need changes:
   ```csharp
   protected InstitutionDto GetDefaultInstitution() => Institutions.GetDefaultInstitution();
   protected TK SendRequest<T, TK>(...) => Eft.SendRequest<T, TK>(...);
   ```

2. **TestContainerBuilder** — owns DI bootstrapping and DB lifecycle. Constructor builds everything, returns `(IServiceProvider, DbConnection?)` tuple. No test logic here.

3. **TestDataSeeder** — owns all seed data creation. Takes `IServiceProvider`, `IRepositoryWrapper`, `ServicesWrapper`, and domain fixtures via primary constructor. Single public method: `void Seed()`.

4. **Domain Fixtures** — group related helpers by domain concept (e.g. `InstitutionFixtures` for institution CRUD/lookup). Use primary constructor injection with `IRepositoryWrapper` and `ServicesWrapper`.

5. **Feature Fixtures** — when a group of tests needs shared domain setup beyond what TestBase provides, extract a `<Feature>Fixtures` class. It takes `TestBase` as a constructor parameter (using `protected internal` visibility on needed members) and owns all feature-specific logic: entity creation, orchestration, verification.

6. **Feature Test Bases** — optional thin facades that inherit `TestBase`, compose a `<Feature>Fixtures` instance, and expose `protected` one-liner delegates. Keeps feature-specific tests clean while reusing all base infrastructure.

7. **Request builders** — isolate request construction, encryption, and sending. Take `IServiceProvider`, `ServicesWrapper`, `IRepositoryWrapper`, and domain fixtures.

8. **Assertion extensions** — use Shouldly extension methods (e.g. `ShouldBeSuccessful()` on `ResponseInfo`) instead of helper methods on base classes.

9. **Never use `Assert.*`** — use Shouldly: `ShouldBe()`, `ShouldNotBeNull()`, `ShouldBeTrue()`, `ShouldContainKey()`, `ShouldBeOfType<T>()`, `ShouldBeEquivalentTo()`.

## When to Apply

- Any test base class exceeding ~150 lines
- When test base handles 3+ unrelated responsibilities (DI, seeding, fixtures, request building, assertions)
- When adding new test infrastructure that doesn't fit cleanly into the existing base
