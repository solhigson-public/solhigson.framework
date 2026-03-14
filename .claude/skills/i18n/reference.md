# Internationalization (.NET) — Detailed Reference

ASP.NET Core localization implementation patterns.

## NuGet Packages
- Built into `Microsoft.AspNetCore.Localization` and `Microsoft.Extensions.Localization` — no additional packages required

## Service Registration
```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
```

## Middleware Configuration
```csharp
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "fr", "pt")
    .AddSupportedUICultures("en", "fr", "pt"));
```
- MUST register BEFORE MVC middleware
- Server location is irrelevant — middleware sets culture per-request from user preference

## Resource File Organization
- Path convention: `Resources/Views/{ControllerName}/{ViewName}.{culture}.resx`
- Example: `Resources/Views/Events/Index.fr.resx`
- Default (no culture suffix) serves as fallback: `Resources/Views/Events/Index.resx`

## Controller Usage
```csharp
public class EventsController(IStringLocalizer<EventsController> localizer) : Controller
{
    public IActionResult Details(int id)
    {
        var message = localizer["EventNotFound"];
        // ...
    }
}
```

## View Usage
```html
@inject IViewLocalizer Localizer
<h1>@Localizer["WelcomeMessage"]</h1>
```

## Date and Number Formatting
- MUST use culture-aware formatting: `value.ToString("d", CultureInfo.CurrentCulture)`
- `CultureInfo.CurrentCulture` is set by `RequestLocalizationMiddleware` per request
- MUST NOT use `CultureInfo.InvariantCulture` for user-facing output — invariant culture produces non-localized formats
- For internal/machine-readable output (logs, APIs, serialization), `InvariantCulture` is correct
