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
