---
name: Cooperative Cancellation
description: "Cancellation token propagation — named argument syntax requirement, mandatory pass-through to all downstream async calls, structured cancellation for parallel operations"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - writing performance-sensitive code
---

# Cooperative Cancellation

Every async service method MUST accept and propagate a cancellation signal. MUST NOT ignore cancellation — MUST pass to all downstream calls (data access, HTTP, file I/O). Parallel operations MUST use structured cancellation — one failure cancels siblings.

MUST ALWAYS pass cancellation tokens using **named argument syntax** — positional passing is PROHIBITED due to silent misrouting when optional parameters precede the token. MUST ALWAYS pass the token when a called method accepts one — MUST NOT call a cancellable method without the token.

For implementation patterns, MUST invoke the `performance` skill.
