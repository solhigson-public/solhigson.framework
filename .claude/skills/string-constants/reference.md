# String Constants — Reference

## What MUST Be a Named Constant

- Error/validation messages returned by service/business logic layers
- Template placeholder names (email, notification, report templates)
- User-facing messages set programmatically (flash messages, toasts, alerts)
- Default values used in method signatures (pagination, timeouts, limits)
- Any string literal used in 2+ files

## What Does NOT Need a Constant

- View/template display text (HTML content, widget labels, Razor markup)
- Single-use log messages with unique contextual data
- Expressions that already reference a constant or language equivalent (`nameof()`, enum `.ToString()`)
- String literals used once in a single file for a single purpose

## Organization

- MUST group constants by concern (error messages, template placeholders, user messages, defaults)
- MUST use parameterized factory methods for recurring message patterns — centralize the FORMAT, not every instance
- For stack-specific file locations and naming conventions, MUST follow the stack's `string-constants.md` rule

---

## .NET File Locations (Domain Layer)

- **`ErrorMessages.cs`** — service/business logic error and validation messages
- **`EmailPlaceholders.cs`** — email template `[[placeholder]]` names
- **`UserMessages.cs`** — user-facing flash messages (controller `SetErrorMessage`/`SetInfoMessage`)
- **`Constants.Pagination`** — extend existing nested class with default values

## .NET Code Patterns

- MUST use `public static string` methods for parameterized messages (3+ uses of same pattern with different entities)
- MUST use `public const string` for fixed, non-parameterized messages
- MUST use nested `static class` per entity/aggregate for unique messages (e.g., `ErrorMessages.Event`, `ErrorMessages.Ticket`)
- MUST use PascalCase for all constant and method names

## Test Assertions

- Entity-specific `const string` messages: MUST assert with the constant reference (`ErrorMessages.Ticket.AlreadyCheckedIn`)
- Parameterized methods: MUST use substring match for the pattern (`"not found"`, `"Not authorized"`)
