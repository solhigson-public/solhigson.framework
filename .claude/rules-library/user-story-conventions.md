---
name: User Story Conventions
description: "User story authoring format — US-{AREA}-{NNN} ID scheme, As-a/I-want/So-that structure, 3-7 Given/When/Then acceptance criteria"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing user stories
  - planning implementation
depends_on:
  - development-workflow
---

# User Story Conventions

## ID Format

- `US-{AREA}-{NNN}` — area is a short feature code (e.g., `AUTH`, `EVT`, `CHK`, `ADMIN`)
- MUST NEVER reuse IDs — retired stories keep their ID with a `Deprecated` note

## Structure

- MUST use "As a / I want / So that" format
- MUST include a "So that" clause — stories without business value justification MUST NOT proceed to implementation
- MUST have 3-7 acceptance criteria per story — fewer than 3 is underspecified, more than 7 means MUST split the story
- MUST use Given/When/Then format for acceptance criteria
- MUST NOT write system user stories — cross-cutting concerns (auth, logging, resilience) are captured in rules and NFRs, not stories
- NFR checks MUST be referenced by NFR-ID in acceptance criteria (see `nfr-conventions.md`), MUST NOT duplicate thresholds across stories

## Priority Definitions

| Priority | Meaning | Example |
|----------|---------|---------|
| **P0** | Critical path — system unusable if broken | Login, payment processing, core CRUD |
| **P1** | Major feature — significant degradation | Search, filtering, role-based access |
| **P2** | Standard feature — workaround exists | Sorting, bulk actions, preferences |
| **P3** | Minor / cosmetic — low user impact | Tooltips, alignment, hover states |
| **P4** | Edge case / nice-to-have | Obscure browser quirks, extreme input lengths |

## File Locations

- Stories: `docs/stories/{area}/` — one file per story
- Template: `.claude/templates/user-story-template.md`
