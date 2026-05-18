---
name: Feature Flags (.NET)
description: "Feature flag strategy — Microsoft.FeatureManagement for rollout/gates/targeting, AppSettingsBase booleans for simple toggles, FeatureGate attribute, feature tag helper"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - writing deployment config
depends_on:
  - feature-flags
---

# Feature Flags (.NET)

MUST use `Microsoft.FeatureManagement` for features requiring any of: percentage-based rollout (A/B testing), time-window gates, or user/group targeting. Use `AppSettingsBase` booleans for simple on/off feature toggles (no rollout required). Both coexist — different `IConfiguration` sections.

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
