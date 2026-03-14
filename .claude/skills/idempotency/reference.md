# Idempotency Patterns — Detailed Reference

Mutation APIs MUST be idempotent — the same request processed twice produces the same result without duplicate side effects.

## API Mutations
- MUST accept an `Idempotency-Key` header (or equivalent field) on every mutation endpoint
- Idempotency check MUST be the FIRST operation in the handler — before any business logic
- On duplicate key: MUST return the existing result, MUST NOT re-execute the operation
- MUST store idempotency keys with a unique index — MUST reject duplicates at the database level

## External References
- MUST derive references for external systems deterministically: `"{entityId}:{operationType}"` (e.g., `"ord-123:CREATE"`)
- Same inputs MUST ALWAYS produce the same external reference
- MUST NEVER generate random UUIDs for references sent to external systems

## Atomicity
- MUST use upsert pattern (INSERT with ON CONFLICT / MERGE) for idempotency checks — MUST NOT use read-then-write
- Read-then-write creates a race condition window where concurrent requests both pass the check
