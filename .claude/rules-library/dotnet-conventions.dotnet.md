---
name: .NET / C# Conventions
description: "Core .NET/C# style — file-scoped namespaces, PascalCase/UPPER_SNAKE_CASE naming, record types for entities/DTOs, Razor views, Hangfire, .NET Aspire conventions"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
---

# .NET / C# Conventions

ASP.NET Core / C# (.NET 9+, slnx format), Razor views, jQuery/JS, Azure, Hangfire, .NET Aspire.

## Tech Stack
- ASP.NET Core / C# (.NET 9+, slnx format), Razor views, jQuery/JS, Azure, Hangfire, .NET Aspire

## Naming & Style
- MUST use file-scoped namespaces (`namespace X;`)
- MUST use PascalCase for public members, `_camelCase` for private fields
- **Acronyms**: MUST capitalize only first letter — `RealityTv`, `DjMusic`, `HttpClient` (not `RealityTV`, `DJMusic`, `HTTPClient`)
- MUST NEVER use `Vm` — MUST ALWAYS use `ViewModel` in full
- MUST use `record` types for entities, DTOs, and value objects — exempt when inheriting from a framework base `class`
- Constants MUST use UPPER_SNAKE_CASE

## DI Patterns
- Controllers: MUST use `public ServicesWrapper ServicesWrapper { get; set; }` (property injection via Autofac)
- MUST use **primary constructor injection** for non-circular deps
- DI registration: MUST comply with `architecture.dotnet.md`

## Async Patterns
- After `Task.WhenAll`, MUST use `await task` (MUST NOT use `task.Result`) to extract results — re-awaiting a completed task is free and unwraps exceptions properly
- MUST NEVER use `task.Result` or `task.Wait()` on incomplete tasks — blocks the thread and risks deadlock

## Core Type Patterns
- Entities: MUST use `record` inheriting `EntityBase`, string IDs via `MassTransit.NewId.NextSequentialGuid()`
- Mapster: MUST use `request.Adapt(entity)` for create/update, `ProjectToType<TDto>()` for read
- DTOs: MUST use `record` types, `{Entity}Dto` naming, `[Required]`/`[StringLength]` annotations
- ViewModels: MUST be in `Domain/ViewModels/` namespace

## Web Conventions
- MUST use Razor views, `ViewBag.Title`/`ViewBag.MetaDescription`/`ViewBag.Robots` for SEO
- MUST use email templates with `[[placeholder]]` syntax
- MUST use jQuery for client-side, semantic HTML5

## Idempotency Patterns

For idempotency implementation (action filter, entity, deterministic references, atomic checks), refer to `idempotency.md`.

## State Machine Patterns

For state machine implementation (transition definitions, service methods, optimistic concurrency), refer to `state-machines.md`.

## Resilience Patterns (Polly v8)

For resilience implementation (retry policies, circuit breakers, non-HTTP resilience, configuration), refer to `resilience.md`.
