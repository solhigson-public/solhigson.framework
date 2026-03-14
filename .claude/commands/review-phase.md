---
description: Pre-commit quality gate — run before committing a completed implementation phase.
---

Review the current phase for completeness and quality before committing.

## Governed By

- `development-workflow.md` — phase splitting, compliance checks, commit discipline

## Procedure

1. **Plan compliance** — MUST read the active plan file. MUST verify every implementation step is complete. MUST flag any steps that were skipped or partially done.

2. **Rule compliance** — for each file changed in this phase, MUST identify which rules apply (architecture, service patterns, naming, auth, error handling, etc.). MUST read each applicable rule and MUST verify conformance. MUST list any violations found.

3. **Code cleanliness** — MUST grep changed files for `TODO`, `FIXME`, `HACK`, `TEMP`, `XXX`. MUST flag any that are not deferred to a future phase in the plan's "Out of scope" section.

4. **Code formatting** — MUST run the stack's formatter in verify/check mode. If it reports changes needed, MUST run the formatter to fix them, then MUST re-verify.

5. **Build gate** — MUST run the stack's build command. MUST pass with zero errors. MUST flag new warnings that indicate incorrect behavior (type mismatches, null reference risks, unused variables that shadow intent). Cosmetic warnings (naming style, whitespace) MUST be deferred.

6. **Scope check** — MUST compare files changed (via `git diff --name-only`) against the plan's scope section. MUST flag any files changed that are NOT listed in the plan scope. Unexpected changes MUST be flagged as potential scope creep.

7. **Report** — MUST present findings grouped by category. MUST NOT confirm ready to commit if any check fails. MUST list all fixes required before the commit can proceed.
