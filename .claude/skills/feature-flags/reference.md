# Feature Flags (.NET) — Detailed Reference

ASP.NET Core implementation using Microsoft.FeatureManagement.

## NuGet Package
- `Microsoft.FeatureManagement.AspNetCore`

## Registration
```csharp
builder.Services.AddFeatureManagement();
```

## Configuration
Feature flags live in the `FeatureManagement` section of `IConfiguration` (appsettings.json, Azure App Configuration, etc.):
```json
"FeatureManagement": {
    "NewCheckoutFlow": {
        "EnabledFor": [
            { "Name": "Microsoft.Percentage", "Parameters": { "Value": 25 } }
        ]
    },
    "EnableBulkExport": true
}
```

## Built-in Filters
- **Percentage** (`Microsoft.Percentage`): enable for N% of evaluations
- **TimeWindow** (`Microsoft.TimeWindow`): enable between `Start` and `End` ISO timestamps
- **Targeting** (`Microsoft.Targeting`): enable for specific users, groups, or a default percentage

## Controller Gating
```csharp
[FeatureGate("NewCheckoutFlow")]
public IActionResult NewCheckout() { ... }
```
- Returns 404 when the flag is off — MUST handle gracefully in client-facing scenarios

## Razor Tag Helper
```html
<feature name="NewCheckoutFlow">
    <p>New checkout experience</p>
</feature>
```

## Service Injection
```csharp
public class CheckoutService(IFeatureManager featureManager)
{
    public async Task<bool> UseNewFlow()
        => await featureManager.IsEnabledAsync("NewCheckoutFlow");
}
```

## Coexistence with AppSettingsBase
- Feature flags use `IConfiguration["FeatureManagement:*"]`
- AppSettingsBase uses its own dedicated section
- No conflict — they operate on separate configuration paths
- MUST NOT use AppSettingsBase for features needing rollout controls (percentage, targeting)

## Open-Source Self-Hosted Alternatives
- Unleash, Flagsmith, Flipt, FeatBit, GrowthBook
- All support the OpenFeature standard for vendor-neutral migration
- Use when centralized flag management, audit logs, or multi-service coordination is needed
