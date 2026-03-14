---
name: idempotency
description: Idempotency patterns for mutation APIs — idempotency keys, deterministic external references, and atomic upsert checks.
---

# Idempotency Patterns

Detailed conventions for making mutation APIs idempotent. Covers idempotency key handling, deterministic external reference generation, and atomic duplicate detection.

## When This Skill Is Invoked
- When implementing or modifying mutation API endpoints
- When integrating with external payment or messaging systems that require idempotent references
- When reviewing idempotency handling in existing code

## Quick Reference
- **Idempotency-Key**: MUST accept on every mutation, MUST check FIRST before business logic
- **External references**: deterministic `"{entityId}:{operationType}"`, NEVER random UUIDs
- **Atomicity**: upsert pattern (INSERT ON CONFLICT), NEVER read-then-write

MUST read `reference.md` for full directive details and implementation patterns.
