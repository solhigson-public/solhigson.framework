# Test Patterns

When creating or refactoring test infrastructure, MUST follow these conventions. For full examples, fixture templates, DI container setup, and mock patterns, MUST invoke the `dotnet-test` skill.

- **TestBase MUST be a thin facade** — composes fixture classes, exposes protected one-liner delegates
- **TestContainerBuilder** MUST mirror production DI with full Autofac container, SQLite in-memory DB
- **TestDataSeeder** MUST use single `Seed()` method, MUST take fixtures via primary constructor
- **Domain Fixtures** — MUST group helpers by domain concept (e.g. `UserFixtures`)
- **Feature Fixtures** MUST take `TestBase` as constructor parameter for shared domain setup beyond TestBase
- MUST use **hand-written mocks** with `[DependencyInject]` for stateful dependencies; NSubstitute MUST only be used for trivial no-op interfaces
- MUST NEVER use `Assert.*` — MUST use Shouldly exclusively: `ShouldBe()`, `ShouldNotBeNull()`, `ShouldBeTrue()`, etc.
- MUST decompose when test base exceeds 150 lines or handles 3+ responsibilities
- For guided test generation from a service class, MUST use `/test-service`
