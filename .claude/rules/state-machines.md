# State Machine Integrity

For entities with lifecycle states (orders, tickets, approvals, subscriptions, etc.).

- MUST define all valid transitions explicitly — MUST NOT directly assign status fields
- MUST use optimistic concurrency on state fields
- For state machine implementation patterns, MUST invoke the `state-machines` skill
