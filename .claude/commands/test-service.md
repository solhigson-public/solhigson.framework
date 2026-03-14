---
description: Generate unit tests for an ASP.NET Core service using xUnit and Shouldly.
---

Generate unit tests for the specified service. MUST follow the conventions in the `dotnet-test` skill — MUST invoke it first for the full test infrastructure reference (TestBase, TestContainerBuilder, fixtures, mocking strategy).

## Governed By

- `test-pattern.md` — test infrastructure, assertion conventions, mock patterns

## Steps

1. **Read existing test infrastructure** — MUST check for TestBase, fixtures, and seeder in the test project. If none exist, MUST set them up per the `dotnet-test` skill before writing tests.

2. **Create test class** — MUST create `{ServiceName}Tests` inheriting TestBase, mirroring the source folder structure in the test project.

3. **Generate tests** for each service method:
   - **Happy path**: valid input → `response.IsSuccessful.ShouldBeTrue()`, correct data
   - **Failure cases**: invalid/null input, entity not found, exception handling
   - **Ownership validation**: if the method validates user ownership, MUST test both owner and non-owner

4. **Naming**: `MethodName_Scenario_ExpectedResult` (e.g., `GetUserAsync_ValidId_ReturnsSuccess`)

5. **Assertions**: MUST use Shouldly exclusively — MUST NOT use `Assert.*`

MUST check existing test files in the project first and MUST follow their patterns.
