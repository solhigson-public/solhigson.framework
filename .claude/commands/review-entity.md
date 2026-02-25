---
description: Review an entity or DTO for convention compliance.
---

Review this entity/DTO for compliance with project conventions:

## Entity Rules (Domain layer)
- Must be a `record` type (not `class`)
- Must inherit `EntityBase` (which provides `Id`, `Created`, `Updated`)
- `Id` must be `string` type, stored as `VARCHAR(450)`
- IDs generated via `MassTransit.NewId.NextSequentialGuid()` — never `Guid.NewGuid()`
- File-scoped namespace: `namespace X;`

## Data Annotations
- `[Required]` on non-nullable fields
- `[StringLength(n)]` with appropriate max lengths — never unbounded `nvarchar(max)`
- `[Column(TypeName = "VARCHAR")]` or `[Column(TypeName = "VARCHAR(n)")]` for string columns
- `[CachedProperty]` on properties that participate in caching
- `[AuditIgnore]` on properties excluded from audit trail
- Decimal precision: `[Column(TypeName = "decimal(18,2)")]`

## Naming
- Entity: `{Name}` (e.g., `Campaign`, `Organisation`)
- DTO: `{Name}Dto` (e.g., `CampaignDto`, `OrganisationDto`)
- ViewModel: `{Name}ViewModel` — **never** `{Name}Vm`
- Properties: PascalCase, private fields: `_camelCase`

## Immutability
- Prefer `required` properties and `init` setters where possible
- Use `record` positional syntax or property declarations — be consistent within the entity

## Relationships
- Use explicit joins in LINQ queries, not navigation properties for complex queries
- Foreign key properties: `{RelatedEntity}Id` as `string`
- No bidirectional navigation properties unless necessary

## DTO Rules
- Must be a `record` type
- Must live in appropriate namespace (`Domain/ViewModels/` or DTO namespace)
- Should contain only the fields needed for the use case — no full entity exposure
- Use Mapster for mapping: `entity.Adapt<Dto>()`, `model.Adapt(entity)`

## Generated Files
- If this entity has a `.generated.cs` partial — never edit it
- Custom logic goes in the non-generated partial class

Flag any violations and suggest fixes with code examples.
