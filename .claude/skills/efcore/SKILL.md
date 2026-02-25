---
name: efcore-data-helper
description: Handle EF Core entities, repositories, queries, and database operations safely following project conventions.
---

# EF Core Data Helper Skill

When working with EF Core, entities, and database operations:

## Entity Rules
- Entities are `record` types inheriting `EntityBase`
- String IDs (`string Id`), stored as `VARCHAR(450)`
- Sequential GUIDs via `MassTransit.NewId.NextSequentialGuid()`
- Data annotations: `[Required]`, `[StringLength]`, `[Column(TypeName = "VARCHAR")]`
- `[CachedProperty]` for cache-participating properties
- `[AuditIgnore]` for audit-excluded properties

## Generated Files
- NEVER edit `.generated.cs` files — they are overwritten by `Solhigson.Framework.efcoretool`
- Place custom code in the corresponding non-generated partial class

## Query Conventions
- `AsNoTracking()` on ALL read-only queries
- Use `Select()` projections for read-only data — never retrieve full entities unnecessarily
- Explicit joins in LINQ (not navigation properties) for complex queries
- Always async: `ToListAsync()`, `FirstOrDefaultAsync()`, etc.
- Pagination via `ToPagedListAsync()` returning `PagedList<T>`
- Caching via `FromCacheListAsync()`, `FromCacheSingleAsync()`

## Read/Write Split
- `AppDbContext` (primary) for writes
- `ReadOnlyAppDbContext` (replica) for read queries
- Route through `RepositoryWrapper`

## Performance Rules
- Never load entire collections to filter in memory
- Use `Include()` or explicit joins for related data — avoid N+1
- Use `AsSplitQuery()` when multiple collection `Include()` would cause Cartesian explosion
- `AddRange()` for batch inserts — never loop `SaveChanges()`
- Specify string lengths (`MaxLength(n)`) — never unbounded `nvarchar(max)`
- Specify decimal precision: `decimal(18,2)`

## Repository Pattern
- `IRepositoryWrapper` aggregates all repositories + `DbContext`
- Repository base: `{AppName}RepositoryBase<T>`, `{AppName}CachedRepositoryBase<T>`
- Interfaces in `Infrastructure/Repositories/Abstractions/`

## Security
- LINQ queries only — never string-concatenated SQL
- If raw SQL unavoidable: `FromSqlRaw(@"...", param)` with parameters
- Validate inputs before query layer
