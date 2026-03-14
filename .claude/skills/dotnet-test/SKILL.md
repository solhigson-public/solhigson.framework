---
name: dotnet-test
description: Test infrastructure, decomposition patterns, DI container setup, database mocking, and assertion conventions for .NET projects.
---

# .NET Test Infrastructure Skill

Covers test project setup: TestBase decomposition, DI container wiring (Autofac + SQLite), mock strategy, seed data, fixtures, and assertion conventions. For application code patterns, use `dotnet-app`. For data layer patterns, use `efcore`.

## When This Skill Is Invoked

- Creating or refactoring test infrastructure (TestBase, fixtures, mocks)
- Setting up a new test project
- Writing test data seeders or domain fixtures
- Reviewing test conventions

## Key Conventions

- **xUnit** for framework, **Shouldly** for assertions (NEVER `Assert.*`), **NSubstitute** for trivial stubs only
- **SQLite in-memory** for DB tests (not EF Core InMemory provider)
- **Full Autofac container** mirroring production DI
- **TestBase is a thin facade** — composes fixture classes, exposes protected one-liner delegates
- **Hand-written mocks** with `[DependencyInject]` for stateful/complex dependencies
- **NSubstitute** only for stateless no-op interfaces

MUST follow `reference.md` for detailed patterns, fixture structure, container setup, and mock examples.
