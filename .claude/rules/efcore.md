EF Core rules that apply to all projects:

**Querying:**
- `AsNoTracking()` on ALL read-only queries
- Use `Select()` projections for read-only data — never retrieve full entities unnecessarily
- Always async: `ToListAsync()`, `FirstOrDefaultAsync()`, never synchronous variants
- Never load entire collections to filter in memory — filter in the database
- Pagination via `ToPagedListAsync()` — never return unbounded result sets

**Performance:**
- Use `Include()` or explicit joins — avoid N+1 queries
- Use `AsSplitQuery()` when multiple collection `Include()` would cause Cartesian explosion
- `AddRange()` / `AddRangeAsync()` for batch inserts — never `SaveChanges()` in a loop
- Specify `[StringLength(n)]` — never unbounded `nvarchar(max)` unless genuinely needed
- Specify decimal precision: `[Column(TypeName = "decimal(18,2)")]`

**Read/Write Split:**
- `AppDbContext` for writes, `ReadOnlyAppDbContext` for reads
- Route through `RepositoryWrapper`

**Security:**
- LINQ queries only — never string-concatenated SQL
- If raw SQL unavoidable: `FromSqlRaw(@"...", param)` with parameters

**Entities:**
- `record` types inheriting `EntityBase`
- String IDs via `MassTransit.NewId.NextSequentialGuid().ToString()`
- `[CachedProperty]` on cache-participating properties
- `[AuditIgnore]` on audit-excluded properties
