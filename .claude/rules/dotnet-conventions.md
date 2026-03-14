# .NET / C# Conventions

ASP.NET Core / C# (.NET 9+, slnx format), Razor views, jQuery/JS, Azure, Hangfire, .NET Aspire. MUST use file-scoped namespaces, PascalCase for public members, `_camelCase` for private fields. MUST use `record` types for entities, DTOs, and value objects.

For detailed service, controller, repository, naming, DI, and async patterns, MUST invoke the `dotnet-app` skill. For controller reviews, MUST use `/review-controller`. For entity/DTO reviews, MUST use `/review-entity`. For EF Core patterns, MUST invoke the `efcore` skill. For test infrastructure, MUST invoke the `dotnet-test` skill.
