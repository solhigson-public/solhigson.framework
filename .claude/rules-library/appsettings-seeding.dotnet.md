---
name: AppSettings Seeding Pattern (.NET)
description: "AppSettingsBase property seeding lifecycle — GetConfiguration with explicit defaults, SeederService initialization, admin page edit-only workflow, known seeding gaps"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - writing deployment config
---

# AppSettings Seeding Pattern

Settings are defined in code via `AppSettingsBase` subclass properties. The `SeederService.InitializeSettingsAsync()` method discovers all `AppSettingsBase` types at startup, reads every property (triggering DB seeding), and cleans up orphaned settings not in code.

## Lifecycle

1. **Define** — add a property to an `AppSettingsBase` subclass in `Infrastructure/ApplicationSettings/`
2. **Seed** — the seeder reads the property on startup, framework writes it to `SolhigsonAppSettings` table with the default value
3. **Edit** — admin edits the value via the Admin > App Settings page (`/admin/app-settings`)

## Rules

- MUST use `GetConfiguration(nameof(PropertyName), "defaultValue")` (non-generic overload with string default) for all new string properties. The generic `GetConfiguration<string>(name)` without a default does NOT seed to DB.
- MUST provide an explicit default for every property — even `""` for keys that start empty. Properties without defaults are invisible to the admin page.
- The Admin App Settings page is edit-only by design — there is no "Add New" button. All settings originate from code.
- MUST NOT manually insert settings into `SolhigsonAppSettings` as the primary workflow. Manual DB inserts are a workaround for framework seeding gaps, not the intended pattern.
- When adding new settings that need credential values, the workflow is: add property in code -> build -> restart app (seeder runs) -> edit value in admin page.

## Known Seeding Gaps

- `GetConfiguration<string>(name)` without default: THROWS `Exception("Configuration [key] not found")` when the key is not in `IConfiguration` (JSON files) or DB. The seeder's `catch { }` swallows the exception, stopping initialization for ALL remaining properties across ALL remaining settings classes. One missing default can prevent dozens of unrelated settings from seeding.
- `IConfiguration` takes priority over DB: if a key exists in `appsettings.json` or `appsettings.{env}.json`, the framework returns it directly without writing to DB. Settings managed via admin page MUST NOT have entries in JSON config files — remove them so the DB seed path executes.
- When adding new settings that need credential values, the workflow is: add property in code -> build -> restart app (seeder runs) -> edit value in admin page.
