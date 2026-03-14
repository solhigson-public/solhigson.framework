# Service Layer Patterns

Services MUST inherit `ServiceBase`, MUST return `ResponseInfo` / `ResponseInfo<T>`. Request DTOs MUST use `record` types with `init` setters and `required` on mandatory fields. For guided endpoint creation, MUST use `/add-endpoint`.

For CRUD patterns (create, update, delete, ownership validation, slug generation), full examples, and code templates, MUST invoke the `dotnet-app` skill.
