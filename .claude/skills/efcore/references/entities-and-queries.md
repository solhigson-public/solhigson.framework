# Entities & Queries

## Contents

- [EF Core Patterns Reference](#ef-core-patterns-reference)
  - [Entity Template](#entity-template)
  - [Querying Rules](#querying-rules)
  - [Read/Write Split](#readwrite-split)
  - [Security](#security)
  - [Entity Conventions](#entity-conventions)
  - [Read Query (with projection)](#read-query-with-projection)
  - [Read Query (with Mapster projection)](#read-query-with-mapster-projection)
  - [Read Query (with explicit join)](#read-query-with-explicit-join)
  - [Cached Query](#cached-query)
  - [Write Operation](#write-operation)
  - [Batch Insert](#batch-insert)
  - [Common Anti-Patterns to Avoid](#common-anti-patterns-to-avoid)

---

## EF Core Patterns Reference

### Entity Template
```csharp
namespace AppName.Domain.Entities;

public record Order : EntityBase
{
    [Required]
    [StringLength(200)]
    [Column(TypeName = "VARCHAR(200)")]
    public required string Title { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(450)]
    [Column(TypeName = "VARCHAR(450)")]
    public string? CustomerId { get; set; }

    [CachedProperty]
    [StringLength(100)]
    public required string Status { get; set; }

    [AuditIgnore]
    public string? InternalNotes { get; set; }
}
```

### Querying Rules
- MUST use `AsNoTracking()` on ALL read-only queries
- MUST use `Select()` or `ProjectToType<TDto>()` projections — MUST NEVER retrieve full entities for reads
- MUST ALWAYS use async: `ToListAsync()`, `FirstOrDefaultAsync()`, NEVER synchronous variants
- MUST NEVER load entire collections to filter in memory — MUST filter in the database
- Pagination via `ToPagedListAsync()` — MUST NEVER return unbounded result sets
- MUST use `Include()` or explicit joins — MUST avoid N+1 queries
- MUST use `AsSplitQuery()` when multiple collection `Include()` would cause Cartesian explosion
- MUST use `AddRange()` / `AddRangeAsync()` for batch inserts — MUST NEVER `SaveChanges()` in a loop
- MUST specify `[StringLength(n)]` — MUST NEVER use unbounded `nvarchar(max)` unless genuinely needed
- MUST specify decimal precision: `[Column(TypeName = "decimal(18,2)")]`

### Read/Write Split
- MUST use `AppDbContext` for writes, `ReadOnlyAppDbContext` for reads
- MUST route through `RepositoryWrapper`

### Security
- MUST use LINQ queries only — MUST NEVER use string-concatenated SQL
- If raw SQL unavoidable: MUST use `FromSqlRaw(@"...", param)` with parameters

### Entity Conventions
- `record` types inheriting `EntityBase`
- String IDs via `MassTransit.NewId.NextSequentialGuid().ToString()`
- `[CachedProperty]` on cache-participating properties
- `[AuditIgnore]` on audit-excluded properties

### Read Query (with projection)
```csharp
var orders = await RepositoryWrapper.DbContext.Orders
    .AsNoTracking()
    .Where(x => x.CustomerId == customerId)
    .Select(x => new OrderDto
    {
        Id = x.Id,
        Title = x.Title,
        Amount = x.Amount,
        Status = x.Status
    })
    .ToPagedListAsync(pageNumber, pageSize);
```

### Read Query (with Mapster projection)
```csharp
var orders = await RepositoryWrapper.DbContext.Orders
    .AsNoTracking()
    .Where(x => x.CustomerId == customerId)
    .ProjectToType<OrderDto>()
    .ToPagedListAsync(pageNumber, pageSize);
```

### Read Query (with explicit join)
```csharp
var result = await (
    from o in RepositoryWrapper.DbContext.Orders.AsNoTracking()
    join c in RepositoryWrapper.DbContext.Customers.AsNoTracking()
        on o.CustomerId equals c.Id
    where o.Status == "Active"
    select new OrderWithCustomerDto
    {
        OrderId = o.Id,
        Title = o.Title,
        CustomerName = c.Name
    }
).ToListAsync();
```

### Cached Query
```csharp
var countries = await RepositoryWrapper.DbContext.Countries
    .AsNoTracking()
    .FromCacheListAsync();

var country = await RepositoryWrapper.DbContext.Countries
    .AsNoTracking()
    .Where(x => x.Id == id)
    .FromCacheSingleAsync();
```

### Write Operation
```csharp
var entity = new Order
{
    Id = MassTransit.NewId.NextSequentialGuid().ToString(),
    Title = model.Title,
    Amount = model.Amount,
    CustomerId = model.CustomerId,
    Status = "Pending"
};

await RepositoryWrapper.DbContext.Orders.AddAsync(entity);
await RepositoryWrapper.DbContext.SaveChangesAsync();
```

### Batch Insert
```csharp
var entities = items.Select(item => new OrderItem
{
    Id = MassTransit.NewId.NextSequentialGuid().ToString(),
    OrderId = orderId,
    ProductId = item.ProductId,
    Quantity = item.Quantity
});

await RepositoryWrapper.DbContext.OrderItems.AddRangeAsync(entities);
await RepositoryWrapper.DbContext.SaveChangesAsync();
```

### Common Anti-Patterns to Avoid
```csharp
// BAD: Loading all to filter in memory
var all = await db.Orders.ToListAsync();
var filtered = all.Where(x => x.Status == "Active");

// GOOD: Filter in database
var filtered = await db.Orders
    .AsNoTracking()
    .Where(x => x.Status == "Active")
    .ToListAsync();

// BAD: N+1 query
foreach (var order in orders)
{
    var customer = await db.Customers.FindAsync(order.CustomerId); // N+1!
}

// GOOD: Single query with join
var result = from o in db.Orders
             join c in db.Customers on o.CustomerId equals c.Id
             select new { o, c };

// BAD: SaveChanges in loop
foreach (var item in items)
{
    db.Items.Add(item);
    await db.SaveChangesAsync(); // N round-trips!
}

// GOOD: Batch
await db.Items.AddRangeAsync(items);
await db.SaveChangesAsync(); // 1 round-trip
```

---