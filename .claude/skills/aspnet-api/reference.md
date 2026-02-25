## API & MVC Conventions Reference

### Request Flow
```
Client -> Controller -> ServicesWrapper -> Service -> RepositoryWrapper -> DbContext -> Database
```

### Controller Template (API)
```csharp
[Route("api/[controller]")]
public class OrdersController : ApiBaseController
{
    public ServicesWrapper ServicesWrapper { get; set; }

    [HttpGet("{id}")]
    [Permission("Orders.View")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await ServicesWrapper.OrderService.GetByIdAsync(id);
        return ApiResponse(result);
    }
}
```

### Controller Template (MVC)
```csharp
[Route("orders")]
public class OrdersController : MvcBaseController
{
    public ServicesWrapper ServicesWrapper { get; set; }

    [HttpGet("")]
    [Permission("Orders.View")]
    public async Task<IActionResult> Index()
    {
        var result = await ServicesWrapper.OrderService.GetListAsync();
        return View(result.Data);
    }
}
```

### Service Template
```csharp
public async Task<ResponseInfo<OrderDto>> GetByIdAsync(string id)
{
    var response = new ResponseInfo<OrderDto>();
    try
    {
        var entity = await RepositoryWrapper.DbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            return response.Fail("Order not found");
        }

        return response.Success(entity.Adapt<OrderDto>());
    }
    catch (Exception e)
    {
        this.LogError(e);
    }
    return response.Fail();
}
```

### Auth Attributes
- `[AllowAnonymous]` — public endpoint
- `[Permission("Resource.Action")]` — RBAC permission check
- `[ThrottleByUser]` — rate limit per user
- `[ThrottleByParam]` — rate limit per parameter value
- `[Button("action")]` — disambiguate multiple POST actions on same route

### Flash Messages
```csharp
SetInfoMessage("Operation successful");
SetErrorMessage("Something went wrong");
```

### SEO (MVC Views)
```csharp
ViewBag.Title = "Page Title | AppName";
ViewBag.MetaDescription = "Description for search engines";
ViewBag.Robots = "index, follow";  // or "noindex, nofollow" for auth pages
```
