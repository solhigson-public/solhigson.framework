# Razor View Helpers — Reference

All form controls and buttons in Razor views MUST use `@Html.CustomHelper()` — the project's `HelperFactory` system for consistent, DRY UI components.

## What MUST use CustomHelper

- **Form controls**: `TextBox`, `TextArea`, `Email`, `Url`, `Password`, `Number`, `Date`, `Time`, `DateTimeLocal`, `DropDownList`, `CheckBox`, `RadioButton`
- **Buttons**: `SubmitButton`, `LinkButton`, `SpanButton`, `ModalSubmitButton`, `ModalCloseButton`, `SearchButton`
- Tag helpers for form controls (`asp-for`, `asp-items`, `asp-validation-for`) are PROHIBITED — MUST use the CustomHelper equivalent

## What stays as plain HTML/tag helpers

- **Routing tag helpers**: `asp-action`, `asp-route-*`, `asp-controller` — acceptable on `<a>`, `<form>` elements
- **Structural HTML**: cards, tables, grids, badges, icons — no helper needed
- **Hidden inputs**: plain `<input type="hidden">` or `@Html.Hidden()` — no CustomHelper wrapper exists for these

## Adding new typed helpers

MUST follow the delegation pattern in `HelperFactory.cs`: MUST create a method that delegates to `TextBoxHelper.Render` with the appropriate `type` parameter. `Email()`, `Date()`, `DateTimeLocal()` are reference implementations.

## Composite control extraction

Any UI pattern repeated **2+ times** (same view or cross-view) MUST be extracted:
- **Default vehicle**: HelperFactory composite helper — small parameterized blocks, `StringBuilder`-based
- MUST use Razor partial view instead when the pattern needs full Razor syntax (`@foreach`, `@if`, nested tag helpers) or a strongly-typed ViewModel with multiple properties

When building or reviewing views, MUST proactively identify repeating UI patterns and MUST suggest extraction before repetition accumulates.

## Composite Control Tracking

1. MUST maintain a `.claude/composite-candidates.{project}.md` file per project
2. MUST create the file when the first composite pattern is identified
3. MUST check and update the file during each phase's planning
4. MUST move extracted patterns to the "Extracted" section
