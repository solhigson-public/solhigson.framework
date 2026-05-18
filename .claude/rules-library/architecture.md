---
name: Architecture
description: "Clean Architecture layer boundaries — Domain/Application/Infrastructure/Web dependency rules, adapter placement, DI registration"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - designing architecture
  - writing code
---

# Clean Architecture

## Layer Dependencies

Inner layers MUST NEVER reference outer layers:

- **Domain**: MUST have zero dependencies on other layers. MUST NOT use ORM or Infrastructure types.
- **Application**: MUST depend only on Domain. Defines service interfaces, DTOs, business logic.
- **Infrastructure**: MUST depend on Application + Domain. Implements repositories, database context, external integrations.
- **Presentation** (Web/API): MUST depend on Application (and Domain for shared types). MUST NEVER import Infrastructure types directly for business logic.

## What Lives Where

- **Domain**: Entities, value objects, enums, domain events
- **Application**: Services, DTOs, ViewModels, service wrappers, repository interfaces
- **Infrastructure**: Repository implementations, database context, ORM configurations, generated files
- **Presentation**: Controllers/handlers, views/templates, static assets, startup configuration

## Naming Conventions

- MUST use `{AppName}.{Layer}` pattern for project/module naming
- MUST keep layer names consistent across the solution

## Adapter Boundary (External Integrations)

- MUST NOT make direct calls to external systems (HTTP APIs, payment gateways, email providers) from service/business logic
- External integrations MUST live in Infrastructure behind Application-layer interfaces
- Each external provider MUST get a dedicated adapter class implementing a domain interface
- Adapters MUST map external error codes/responses to domain error types

## DI Registration

- Infrastructure MUST register implementations against Application-layer interfaces
- MUST NEVER resolve Infrastructure types directly from presentation layer
