# Controller & View Patterns

## Contents

- [ViewModel Mapper Pattern](#viewmodel-mapper-pattern)
- [Public Page Error Handling](#public-page-error-handling)
- [Permissions & RBAC](#permissions--rbac)
- [Controller Templates](#controller-templates)

---

## ViewModel Mapper Pattern

For directives (static class, placement, per-area mapping, thin controller), MUST comply with `viewmodel-mapper-pattern.md`.

### Example
```csharp
public static class PublicViewModelMapper
{
    public static EventBrowseCardViewModel ToEventBrowseCard(EventDto dto, string currencySymbol)
    {
        return new EventBrowseCardViewModel(
            Slug: dto.Slug, Title: dto.Title, Category: dto.Category.ToString(),
            Price: FormatPrice(minPrice, currencySymbol)
        );
    }

    public static string FormatPrice(decimal amount, string currencySymbol)
        => $"{currencySymbol}{amount:N0}";

    public static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : (parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : "?");
    }
}
```

---

## Public Page Error Handling

For directives (browse/list, detail, aggregation, checkout error strategies), MUST comply with `public-page-error-handling.md`.

### Browse / List Pages
On service error: set flash message + render page with empty results.
```csharp
var response = await ServicesWrapper.SomeService.GetItemsAsync(page, pageSize);
if (!response.IsSuccessful)
{
    SetErrorMessage("Unable to load items. Please try again.");
}
var items = response.IsSuccessful ? response.Data : new PagedList<SomeDto>();
```
The view's empty-state UI handles "no results." The flash message distinguishes "error" from "nothing matches."

### Detail Pages
Two distinct failure modes — MUST NEVER conflate them:
```csharp
var response = await ServicesWrapper.SomeService.GetBySlugAsync(slug);
if (!response.IsSuccessful)
{
    SetErrorMessage("Unable to load this item. Please try again.");
    return RedirectToAction("Browse");
}
if (response.Data is null)
{
    return NotFound();
}
```
MUST NEVER return 404 for transient errors — the item may exist but the lookup failed.

### Aggregation Pages (Home / Dashboard)
On partial failure: render what succeeded, skip failed sections. No flash messages — empty sections are acceptable on discovery surfaces.

### Checkout / Transactional Pages
- No active order → redirect to home with `SetInfoMessage`
- Order fetch fails → redirect to home with `SetErrorMessage`
- MUST NEVER show a broken checkout page

---

## Permissions & RBAC

### Permission Definition
All permissions in `Permission.cs` static partial class with `PermissionInfo*` attributes:
- `PermissionInfoMenuRoot` — top-level menu group
- `PermissionInfoChildMenu` — visible menu item under a root
- `PermissionInfoChildNonMenu` — action-only permission, no menu entry

Naming: `permission.{area}.{action}` (lowercase, dot-separated).

Source generator converts Description to PascalCase → `Permission.Users = "permission.users.view"`.

### Controller Auth — Mandatory
Every action MUST have one of:
- `[AllowAnonymous]` — explicitly public
- `[Permission(Permission.X)]` — requires specific RBAC permission
- `[Authorize]` — requires authentication only, no role restriction

**Least privilege:** When a matching `Permission.*` constant exists for the action's business domain, MUST use `[Permission(Permission.X)]` instead of `[Authorize]`. `[Authorize]` is appropriate only when the action genuinely applies to ALL authenticated users regardless of role (e.g., toggling a personal favorite). MUST NOT use `[Authorize]` as a convenience shortcut when a scoped permission would be more precise.

**MUST NOT have naked actions.** An action without any of these three attributes is a security bug.

MUST NEVER hardcode permission strings — MUST ALWAYS use generated `Permission.*` constants.

### Enforcement — Build-Time Test
```csharp
[Fact]
public void AllActions_MustHaveExplicitAuthAttribute()
{
    var controllerTypes = typeof(MvcBaseController).Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

    var violations = new List<string>();
    foreach (var controller in controllerTypes)
    {
        var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes().Any(a =>
                a is HttpGetAttribute or HttpPostAttribute or HttpPutAttribute
                or HttpDeleteAttribute or HttpPatchAttribute));

        foreach (var action in actions)
        {
            var hasAuth = action.GetCustomAttributes(true).Concat(controller.GetCustomAttributes(true))
                .Any(a => a is AllowAnonymousAttribute or AuthorizeAttribute or PermissionAttribute);

            if (!hasAuth)
                violations.Add($"{controller.Name}.{action.Name}");
        }
    }

    violations.ShouldBeEmpty($"Actions missing auth attribute: {string.Join(", ", violations)}");
}
```

### Enforcement — Runtime Filter
```csharp
public class RequireExplicitAuthFilter : IAsyncActionFilter
{
    private readonly IWebHostEnvironment _env;
    public RequireExplicitAuthFilter(IWebHostEnvironment env) => _env = env;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var hasAllowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var hasAuthorize = endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null;

        if (!hasAllowAnonymous && !hasAuthorize)
        {
            if (_env.IsDevelopment())
            {
                context.Result = new ContentResult
                {
                    StatusCode = 403,
                    Content = $"This endpoint has no authorization attribute. " +
                              $"Add [AllowAnonymous], [Permission], or [Authorize] to {context.ActionDescriptor.DisplayName}.",
                    ContentType = "text/plain"
                };
            }
            else
            {
                context.Result = new StatusCodeResult(403);
            }
            return;
        }

        await next();
    }
}
```

Register globally:
```csharp
services.AddControllersWithViews(options =>
{
    options.Filters.Add<RequireExplicitAuthFilter>();
});
```

---

### Antiforgery

- MUST register `AutoValidateAntiforgeryTokenAttribute` as a global MVC filter
- MUST NOT use `[ValidateAntiForgeryToken]` on individual actions — global filter handles it
- Actions that receive external callbacks (webhooks, payment notifications) MUST use `[IgnoreAntiforgeryToken]`
- Token delivery for forms: automatic via `@Html.AntiForgeryToken()` hidden input
- Token delivery for AJAX: MUST follow `ajax-pattern.dotnet.md` CSRF section

### Service Call Counting

- MUST NOT call more than one service from a controller action. If a facade is used, all service calls MUST go through the facade.
- Any call to any service counts, including cached/utility services. Cached or fast services are NOT exempt.
- If a controller action calls a facade AND another service, that is 2 calls — MUST move the second call into the facade.

---

## Controller Templates

### MVC Controller
```csharp
[Route("orders")]
public class OrdersController : MvcBaseController
{
    public ServicesWrapper ServicesWrapper { get; set; }

    [HttpGet("")]
    [Permission(Permission.Orders)]
    public async Task<IActionResult> Index()
    {
        var result = await ServicesWrapper.OrderService.GetListAsync();
        return View(result.Data);
    }
}
```

### API Controller
```csharp
[Route("api/[controller]")]
public class OrdersController : ApiBaseController
{
    public ServicesWrapper ServicesWrapper { get; set; }

    [HttpGet("{id}")]
    [Permission(Permission.Orders)]
    public async Task<IActionResult> Get(string id)
    {
        var result = await ServicesWrapper.OrderService.GetByIdAsync(id);
        return ApiResponse(result);
    }
}
```

### Auth Attributes
- `[AllowAnonymous]` — public endpoint
- `[Permission(Permission.X)]` — RBAC permission check
- `[Authorize]` — authentication only
- `[ThrottleByUser]` / `[ThrottleByParam]` — rate limiting
- `[Button("action")]` — disambiguate multiple POSTs on same route

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

---