---
description: Generate unit tests for an ASP.NET Core service using xUnit, NSubstitute, and Shouldly.
---

Generate unit tests for the specified service. Follow these conventions:

## Test Project Structure
- Tests go in the corresponding test project (e.g., `{AppName}.Application.Tests` or `{AppName}.Tests`).
- Mirror the source folder structure in the test project.
- Test class name: `{ServiceName}Tests`.

## Test Framework
- **xUnit** for test framework
- **NSubstitute** for mocking
- **Shouldly** for assertions — use exclusively, never `Assert.*`

## What to Mock
- `IRepositoryWrapper` and its repositories
- `ServicesWrapper` (if the service uses it)
- External service dependencies
- Never mock the service under test itself

## Test Coverage Requirements
For each service method, generate tests for:

### Happy Path
- Valid input returns `response.Success()` with correct data
- Verify correct repository methods were called
- Verify Mapster mapping produces expected output

### Failure Cases
- Invalid/null input returns `response.Fail()`
- Entity not found returns appropriate failure
- Exception in repository is caught, logged, and returns `response.Fail()`

### ResponseInfo Pattern
- `response.IsSuccessful.ShouldBeTrue()` for success cases
- `response.IsSuccessful.ShouldBeFalse()` for failure cases
- `response.Data.ShouldNotBeNull()` and `response.Data.ShouldBe(expected)` for payload
- `response.Message.ShouldContain("expected text")` for error messages

## Test Naming Convention
```
MethodName_Scenario_ExpectedResult
```
Examples:
- `GetUserAsync_ValidId_ReturnsSuccess`
- `GetUserAsync_InvalidId_ReturnsFail`
- `CreateOrderAsync_NullInput_ReturnsFail`
- `CreateOrderAsync_DuplicateEmail_ReturnsFail`

## Template
```csharp
public class {ServiceName}Tests
{
    private readonly IRepositoryWrapper _repoMock;
    private readonly {ServiceName} _sut;

    public {ServiceName}Tests()
    {
        _repoMock = Substitute.For<IRepositoryWrapper>();
        _sut = new {ServiceName}();
        // Wire up RepositoryWrapper via property injection or constructor
    }

    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        // Act
        // Assert — use Shouldly
    }
}
```

Check existing test files in the project first and follow their patterns.
