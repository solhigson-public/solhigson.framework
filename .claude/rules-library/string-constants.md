---
name: String Constants
description: "Magic string elimination — named constant requirements for multi-file or Application-layer strings, classification rules, parameterized factory methods"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
---

# String Constants

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
