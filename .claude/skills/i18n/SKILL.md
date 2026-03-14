---
name: i18n
description: "Internationalization (.NET) — IStringLocalizer, .resx files, RequestLocalizationMiddleware, culture-aware formatting"
user_invocable: true
---

# Internationalization (.NET Implementation)

ASP.NET Core localization using IStringLocalizer and resource files. For principles, see the common `i18n` skill.

## When This Skill Is Invoked
- When configuring RequestLocalizationMiddleware
- When creating .resx resource files for controllers or views
- When injecting IStringLocalizer or IViewLocalizer
- When formatting dates or numbers for user display in .NET

## Stack
- **Localization**: `Microsoft.Extensions.Localization` (IStringLocalizer)
- **View localization**: `Microsoft.AspNetCore.Mvc.Localization` (IViewLocalizer)
- **Middleware**: `Microsoft.AspNetCore.Localization` (RequestLocalizationMiddleware)
- **Resources**: `.resx` files organized by controller/view

## Key Conventions
- Resource path convention: `Resources/Views/{Controller}/{View}.{culture}.resx`
- MUST NOT use `CultureInfo.InvariantCulture` for user-facing output
- Culture is set per-request by middleware — server location is irrelevant

MUST read `reference.md` for registration, middleware setup, and usage patterns.
