---
name: feature-flags
description: "Feature flags (.NET) — Microsoft.FeatureManagement setup, filters, FeatureGate, Razor tag helper, AppSettingsBase coexistence"
user_invocable: true
---

# Feature Flags (.NET Implementation)

ASP.NET Core feature flag implementation using Microsoft.FeatureManagement. For principles and lifecycle, see the common `feature-flags` skill.

## When This Skill Is Invoked
- When adding Microsoft.FeatureManagement to a project
- When configuring feature filters (percentage, targeting, time window)
- When gating controllers or Razor views behind a feature flag
- When deciding between FeatureManagement and AppSettingsBase

## Stack
- **NuGet**: `Microsoft.FeatureManagement.AspNetCore`
- **Configuration**: `FeatureManagement` section in `IConfiguration`
- **Controller gating**: `[FeatureGate]` attribute
- **View gating**: `<feature>` tag helper
- **Service check**: `IFeatureManager.IsEnabledAsync()`

## Key Conventions
- Feature flags use `IConfiguration["FeatureManagement:*"]` — no conflict with AppSettingsBase
- MUST NOT use AppSettingsBase for features needing rollout controls (percentage, targeting)
- Built-in filters: Percentage, TimeWindow, Targeting

MUST read `reference.md` for configuration examples and implementation patterns.
