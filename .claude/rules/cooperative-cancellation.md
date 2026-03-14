# Cooperative Cancellation

Every async service method MUST accept and propagate a cancellation signal. MUST NOT ignore cancellation — MUST pass to all downstream calls (data access, HTTP, file I/O). Parallel operations MUST use structured cancellation — one failure cancels siblings.

MUST ALWAYS pass cancellation tokens using **named argument syntax** — positional passing is PROHIBITED due to silent misrouting when optional parameters precede the token. MUST ALWAYS pass the token when a called method accepts one — MUST NOT call a cancellable method without the token.

For implementation patterns, MUST invoke the `performance` skill.
