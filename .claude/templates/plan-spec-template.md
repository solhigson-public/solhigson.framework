# Plan: [Title]

## Context
<!-- Why is this change needed? What problem does it solve? What prompted it? -->

## Scope
### Files to modify
- `path/to/file` — what changes

### Out of scope
- What this plan explicitly does NOT touch

## Design Decisions
<!-- Choices made during planning and why. Each is a lightweight inline ADR. -->
- **[Decision]** — [rationale]. Alternatives considered: [X, Y].

## Interface Changes
<!-- New or modified public APIs, DTOs, endpoints, DB schema. Skip if none. -->
- **New endpoint**: `POST /api/...` — description
- **New DTO**: `SomethingDto` — fields
- **Schema**: new column `X` on table `Y`

## Implementation Steps
<!-- Numbered, each independently verifiable. Each step MUST include a Verify annotation. -->
1. [ ] Step description
   - Verify: what constitutes a pass (e.g., "builds clean", "test X passes", "file contains Y")
2. [ ] Step description
   - Verify: concrete pass/fail check

## Verification
<!-- Mandatory post-completion checks. MUST execute all items before declaring the plan complete. -->
- [ ] Check 1
- [ ] Check 2

## Test Strategy
<!-- What to test, which test phase covers it. -->
- Unit: ...
- Integration: ...
- Manual verification: ...
