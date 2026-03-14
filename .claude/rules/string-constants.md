# String Constants

Hardcoded string literals used in 2+ files or in service/business logic MUST be named constants. Template placeholders, user-facing messages, and default values MUST be constants. Single-use view text and log messages do NOT need constants.

For classification rules, organization, and parameterized factory methods, MUST invoke the `string-constants` skill.
