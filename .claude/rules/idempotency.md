# Idempotency

Mutation APIs MUST be idempotent — the same request processed twice produces the same result without duplicate side effects.

- MUST accept an `Idempotency-Key` on every mutation endpoint
- MUST derive external references deterministically — MUST NEVER use random UUIDs
- For idempotency implementation patterns, MUST invoke the `idempotency` skill
