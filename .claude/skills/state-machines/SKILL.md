---
name: state-machines
description: State machine integrity patterns for entities with lifecycle states — transitions, guards, concurrency, and audit logging.
---

# State Machine Integrity

Detailed conventions for implementing state machines on entities with lifecycle states (orders, tickets, approvals, subscriptions, etc.). Covers explicit transition definitions, guard preconditions, optimistic concurrency, and transition audit logging.

## When This Skill Is Invoked
- When implementing or modifying entities with lifecycle status fields
- When adding state transitions, guards, or status change logic
- When reviewing state machine patterns in existing code

## Quick Reference
- **Transitions**: explicit `(from-state, to-state)` pairs, no direct status assignment, terminal states have no outbound transitions
- **Guards**: precondition validation before each transition, clear errors on failure
- **Concurrency**: optimistic concurrency on state field, exactly-one-succeeds guarantee
- **Audit**: every transition logged with entity ID, from/to states, trigger/reason, timestamp

MUST read `reference.md` for full directive details and implementation patterns.
