---
description: Review an entity or DTO for convention compliance.
---

Review this entity/DTO for compliance with project conventions.

## Governed By

- `dotnet-conventions.md` — record types, naming, file-scoped namespaces
- `service-patterns.md` — DTO patterns, Mapster mapping
- `generated-files.md` — partial class rules

## Procedure

1. **Type** — MUST verify entity is a `record` type (not `class`). MUST verify it inherits `EntityBase`.

2. **Data annotations** — MUST verify:
   - `[Required]` on non-nullable fields
   - `[StringLength(n)]` with max lengths matching the database column definition — MUST NOT use unbounded `nvarchar(max)`
   - `[Column(TypeName = "VARCHAR(n)")]` for string columns
   - Decimal precision: `[Column(TypeName = "decimal(18,2)")]`

3. **Naming** — MUST verify PascalCase for properties, `_camelCase` for private fields. Entity: `{Name}`, DTO: `{Name}Dto`, ViewModel: `{Name}ViewModel` — MUST NOT abbreviate as `Vm`.

4. **Immutability** — MUST verify `required` properties and `init` setters where possible.

5. **Relationships** — MUST verify explicit joins in LINQ (not navigation properties for complex queries). Foreign keys: `{RelatedEntity}Id` as `string`.

6. **DTO scope** — MUST verify DTOs contain only fields needed for the use case — MUST NOT expose full entity.

7. **Generated files** — MUST verify no edits to `.generated.cs` — custom logic MUST go in the non-generated partial class.

MUST flag any violations and MUST suggest fixes with code examples.
