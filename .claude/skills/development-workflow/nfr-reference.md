# Non-Functional Requirements Conventions — Reference

## ID Format

- `NFR-{CAT}-{NNN}` — category from the NFR category legend (e.g., `PERF`, `SEC`, `ACC`)
- MUST NEVER reuse IDs — retired NFRs keep their ID with a `Deprecated` note

## Structure

- Every NFR MUST have a measurable threshold — "fast" is not a threshold; "p95 < 2s on 4G" is
- Every NFR MUST specify a verification method, tool, and frequency — an NFR without verification is a wish, not a requirement
- MUST NOT use "All" as scope without justification — most NFRs have a meaningful boundary
- Priority: P0 = blocks release, P1 = MUST fix before GA, P2 = post-launch iteration

## Referencing

- Test scripts MUST reference NFR-IDs when verifying non-functional behavior (see `qa-conventions.md`)
- User story acceptance criteria MUST reference NFR-IDs for applicable thresholds (see `user-story-conventions.md`), MUST NOT duplicate thresholds

## File Locations

- NFRs: `.claude/docs/nfr.md` — single file, table-based
- Template: `.claude/templates/nfr-template.md`
