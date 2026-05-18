---
name: Development Workflow
description: "Quality gates (plan → review → implement → code review → commit), phase splitting (15 steps max), unit test requirements, clean build gates, commit discipline, roadmap management, post-phase reflection"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - planning implementation
  - writing QA test cases
  - writing user stories
depends_on:
  - user-story-conventions
  - nfr-conventions
  - qa-conventions
---

# Development Workflow

## Entry Point

All implementation work enters through the `/work` command. The `/work` command owns the lifecycle flow (Input, Discussion, Artifact Recommendation, Plan/Phase, Execute). This rule owns the constraints and gates that apply during execution.

## Artifact Recommendation

During the Discussion step, the main session MUST recommend which artifacts are warranted for the work based on scope and risk:

- **User stories** — per `user-story-conventions.md`
- **NFRs** — per `nfr-conventions.md`
- **QA test scripts** — per `qa-conventions.md`

The main session MUST present the recommendation with rationale. The user decides which artifacts to produce. Artifacts are NOT mandatory gates — work proceeds with whichever artifacts the user approves.

## Implementation Phase Details

### Sub-Phase Splitting
- MUST split when a phase has 2+ distinct controllers to create, 2+ distinct permission groups to add, or 2+ distinct aggregate roots being wired.
- Each sub-phase MUST target exactly one controller/feature domain, build clean independently, and stay under 15 implementation steps.
- Sub-phase naming: `{phase}{letter}-{number}` (e.g., 14c-1, 14c-2, 14c-3).

### Step-by-Step Review
- MUST pause and review after each step — MUST check plan conformance and rule compliance. MUST NOT batch steps.
- Plan `## Verification` sections are mandatory — MUST execute all verification checks before declaring a plan complete.

### Parallel Execution
- When a phase has 3+ clearly independent mechanical steps (e.g., creating multiple templates, wiring multiple services), MAY parallelize via Task agents. Each agent gets fresh context. MUST verify all agents complete before proceeding to dependent steps.

## Test Phases

Unit tests MUST be written by the implementation agent as part of Quality Gate Step 3 — they are inline with implementation, not a separate phase.

Dedicated test phases are for integration and end-to-end tests that require separate infrastructure setup (TestBase, fixtures, mocks, seeder). When applicable:
- Each implementation phase MUST get a dedicated test phase afterward (e.g., Phase 3a → Phase 3a-tests).
- Test phases MUST be separate plans.
- The first test phase MUST set up test infrastructure AND clear the test backlog.
- Subsequent test phases MUST focus on testing the most recent implementation phase.

## Dependency Ordering

- **Foundation first** — MUST implement data models, schema, migrations before service logic.
- **Inner layers before outer** — MUST implement core/domain before application before infrastructure before presentation.
- **Core features before dependent** — MUST implement auth/RBAC before feature-specific permissions.
- **Shared infrastructure before consumers** — MUST implement base classes and templates before feature classes.

## Rule Compliance

After each implementation step, MUST delegate a light compliance check to a Task agent:
1. MUST pass the list of changed files to the agent.
2. The agent MUST read each file and check against all loaded rules.
3. If violations are found, MUST fix before proceeding to the next step. If a rule seems wrong, MUST flag it to the user rather than silently deviating.

For full codebase compliance verification, MUST use `/scan-drift` (deep scan).

## Roadmap Management

The project roadmap lives at `docs/ROADMAP.md`.

- MUST update the roadmap without asking. NEVER include roadmap updates as a plan step.
- MUST bundle roadmap updates into the same commit as the phase work.
- MUST only add or check off items. NEVER remove or merge completed phases.
- A phase is archive-eligible when: all its checkboxes are `[x]`, its title carries the `✅` marker, and its implementation has been committed and verified (review agent PASS or build confirmed).
- When archive-eligible phases exceed 30, MUST move them to `docs/ROADMAP-ARCHIVE.md`, preserving their original phase numbers and completion dates.
- After plan approval: implement, build, commit, present next phase plan. In autonomous mode: MUST NOT stop between phases unless user requests a pause. In manual mode: MUST pause after each phase commit per the `/work` command Step 6.

## Quality Gate

Every implementation change MUST follow this sequence. This gate applies uniformly to all work that produces a git commit — single-phase, multi-phase, any scope. There are no relaxations based on work size or type.

### 1. Plan

Before implementation, the main session MUST produce:
- Scope lock: files allowed to modify
- Acceptance criteria: concrete, verifiable conditions for PASS
- Anti-instructions: files and concerns the agent MUST NOT touch
- QA determination: apply the QA Triggering Policy (below) — result is "QA required" or "QA not required"
- Test determination: implementation MUST include unit tests. The implementation agent MUST cross-reference existing test files before writing new tests.

For single-phase work, the plan is the dispatch prompt to the implementation agent. For multi-phase work, the plan is the phase spec. Both carry identical required elements.

### 2. Review Plan

The main session MUST dispatch an IS design review agent (Opus) to evaluate the plan before implementation begins. The review agent evaluates: scope adequacy, acceptance criteria completeness, risk assessment, and cross-cutting concerns.

### 3. Implement

The main session MUST dispatch an implementation agent (Sonnet) with:
- The plan (scope lock, acceptance criteria, anti-instructions)
- Active rules manifest (relevant rules from the registry)
- Prior phase handoff (if multi-phase; nil otherwise)
- Build state (current commit hash)

The implementation agent MUST:
- Produce a pre-flight statement declaring which files it will touch and which patterns it will apply
- Write code AND unit tests — MUST cross-reference existing test files and conventions before writing new tests
- Run all unit tests (new and existing) — all MUST pass before staging
- Build the solution — MUST produce a clean build with zero errors
- Stage all changes via `git add` — MUST NOT commit

### 4. Review Code

The main session MUST dispatch a review agent (Opus) with:
- The diff: `git diff --cached` (or `git diff --cached -- file1 file2 ...` for scoped review)
- The plan (scope lock, acceptance criteria, anti-instructions)
- Implementation agent's completion notes
- `product-development-roles.md` for excellence gate evaluation
- Build output confirming clean build
- Prior phase handoff (if multi-phase)
- Cumulative context document (if multi-phase)

The review agent MUST return a verdict: PASS, PASS_WITH_FINDINGS, or FAIL.

### 5. Handle Verdict

- **PASS or PASS_WITH_FINDINGS**: proceed to step 7 (Commit). QA runs end-of-mandate, not per-phase.
- **FAIL**: the troubleshooter rule MUST be invoked after the first FAIL. The main session MUST follow the troubleshooter protocol (diagnose root cause, not guess at fix) until resolution. Maximum 3 FAIL cycles. After 3 FAILs, `git reset HEAD` to unstage all changes and escalate to the user with diagnostic findings.

### 6. QA Verification

QA verification runs as a single end-of-mandate pass after all phases complete, not per-phase. See End-of-Mandate QA in `autonomous-execution.md` for the full protocol. Individual phases proceed from review PASS directly to commit without waiting for QA.

### 7. Commit

ONLY after review PASS:
- The main session MUST dispatch a Haiku agent with exact commit message, file list, and git commands
- The Haiku agent executes the commit and push verbatim — it MUST NOT compose messages, select files, or choose flags
- Implementation agents MUST NOT commit — only Haiku agents commit on main session instruction

#### Universal Commit Gate

Before dispatching a Haiku agent to commit, the main session MUST emit:

```
[Commit Gate: Verification required? <type> | Verification passed? yes/no/deferred | Gate open? yes/no]
```

Where `<type>` is the verification that must pass before the gate opens. The gate is closed unless `Verification passed? yes`. No commit type is exempt.

| Commit Type | Verification Required |
|---|---|
| Implementation (UI or backend) | Code review PASS |
| Governance/rules | IS design review PASS or Author review PASS |
| Config/session/docs | Authoring agent completion confirmation — the agent's response includes the file path modified and a statement that the task is complete. The main session verifies the file exists and content matches intent before opening the gate. |

When `Verification passed? deferred`, the gate remains closed. Changes remain staged until verification completes.

**Mixed commits:** When a commit bundles multiple change types (e.g., implementation + roadmap update), the highest verification tier applies. Implementation verification subsumes config/docs verification.

**QA verification:** QA runs as a single end-of-mandate pass after all phases complete, per End-of-Mandate QA in `autonomous-execution.md`. QA does not block commits or pushes. When QA finds issues, fix phases follow the standard commit gate (review PASS = commit + push).

### 8. Completion Summary

After commit, the main session MUST present a structured completion summary to the user. The summary MUST include:
- Files modified (with one-line description per file)
- What changed (functional description, not diff)
- Design decisions made (if any)
- Deferred findings (if any)
- Items the user needs to know (blockers, follow-ups, risks)

For multi-phase autonomous execution, the execution tracker and cumulative context document serve this purpose — the main session MUST NOT duplicate the summary per phase. The summary MUST be presented at mandate completion.

### QA Triggering Policy

QA verification MUST be triggered when the implementation includes ANY of:
1. UI changes (views, layouts, CSS, client-side scripts)
2. User-facing flow changes (auth, forms, navigation, checkout)
3. Existing test scripts in `docs/qa/` that reference the implementation's scope

Backend-only changes (service refactoring, migrations, configuration, domain logic) skip QA verification and proceed directly from review PASS to commit.

## Post-Phase Reflection

After each phase commit, MUST run a lightweight reflection:
- MUST check for patterns worth codifying (repeated solutions, new conventions).
- MUST note operational insights (wasted cycles, effective techniques).
- MUST flag any rule violations encountered during the phase.
- For substantial findings, MUST invoke `/reflect` for full analysis and file updates.

## Phase Count Branching

Phase count branching (1-phase direct execution vs 2+-phase autonomy gate) is defined in the `/work` command. The Quality Gate applies identically regardless of the branching outcome.
