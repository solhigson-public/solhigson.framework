# Feature Flags (.NET)

MUST use `Microsoft.FeatureManagement` for features requiring rollout control. MUST use `[FeatureGate]` attribute on controller actions, `<feature>` tag helper in Razor views. MUST NOT use `AppSettingsBase` booleans for features needing percentage rollout, time windows, or targeting — `AppSettingsBase` is for simple on/off configuration. Both coexist — different `IConfiguration` sections.

For setup, filters, and patterns, MUST invoke the `feature-flags` skill.
