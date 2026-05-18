---
name: String Constants (.NET)
description: "String constant organization — ErrorMessages, EmailPlaceholders, UserMessages, Constants with nested classes per concern, const vs static method selection"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
depends_on:
  - string-constants
---

# String Constants (.NET)

MUST use separate flat classes per category in the Domain layer: `ErrorMessages.cs` (static const strings), `EmailPlaceholders.cs` (parameterized methods), `UserMessages.cs` (UI strings), `Constants.cs` with nested static classes per concern (e.g., `Constants.Pagination`, `Constants.Defaults`). MUST use `public const string` for fixed messages, `public static string` methods for parameterized patterns.

For file locations, code patterns, nested class organization, and test assertion conventions, MUST invoke the `string-constants` skill.
