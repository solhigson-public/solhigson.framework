# State Machine Integrity — Detailed Reference

For entities with lifecycle states (orders, tickets, approvals, subscriptions, etc.).

## Transition Rules
- MUST define all valid transitions explicitly as `(from-state, to-state)` pairs
- MUST NOT directly assign status field — all state changes MUST go through a transition method
- The transition method MUST validate the current state allows the requested transition before applying it
- Terminal states (Completed, Cancelled, Expired) MUST NOT have outbound transitions

## Guards
- MUST validate preconditions before each transition (e.g., payment received before confirming an order)
- Guard failures MUST return a clear error — MUST NEVER silently skip the transition

## Concurrency
- MUST use optimistic concurrency on the state field to prevent conflicting transitions
- If two requests attempt the same transition concurrently, exactly one succeeds

## Audit
- MUST log every transition: entity ID, from-state, to-state, trigger/reason, timestamp
