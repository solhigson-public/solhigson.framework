# Development Workflow — Reference

## Feature Lifecycle Flows

### New Feature — With UX Designs

1. **Discussion** — clarify requirements, identify scope
2. **User stories** — per `user-story-conventions.md`, referencing UX designs
3. **NFRs** — per `nfr-conventions.md` if feature introduces new quality thresholds
4. **Test scripts** — per `qa-conventions.md`, derived from stories + UX designs (concrete UI elements are known)
5. **Plan → Implement → Build → Commit** — per Implementation Phase Details below
6. **Update test scripts** — only if implementation deviated from designs
7. **Execute test scripts** — per test execution tiers in `qa-conventions.md`

### New Feature — Without UX Designs

1. **Discussion** — clarify requirements, identify scope
2. **User stories** — per `user-story-conventions.md`
3. **NFRs** — per `nfr-conventions.md` if feature introduces new quality thresholds
4. **Plan → Implement → Build → Commit** — per Implementation Phase Details below
5. **Test scripts** — per `qa-conventions.md`, derived from stories + actual UI
6. **Execute test scripts** — per test execution tiers in `qa-conventions.md`

### Bug Fix or Change to Existing Feature

1. **Update affected user stories** — if acceptance criteria change
2. **Plan → Implement → Build → Commit** — per Implementation Phase Details below
3. **Update affected test scripts** — if steps or expected results change
4. **Re-execute affected test cases**

### Documenting Existing Features (Backfill)

1. **Write user stories** — from existing code and UI behavior
2. **Write NFRs** — from existing quality targets and rules
3. **Write test scripts** — from stories + actual running UI
4. **Execute test scripts** — establish baseline

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

- Each implementation phase MUST get a dedicated test phase afterward (e.g., Phase 3a → Phase 3a-tests).
- Test phases MUST be separate plans — they go through plan mode just like implementation phases.
- The first test phase MUST set up test infrastructure (TestBase, fixtures, mocks, seeder) AND clear any test backlog.
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

The project roadmap lives at `.claude/ROADMAP.md`.

- MUST update the roadmap without asking. NEVER include roadmap updates as a plan step.
- MUST bundle roadmap updates into the same commit as the phase work.
- MUST only add or check off items. NEVER remove or merge completed phases.
- When "Completed" exceeds ~30 phases, MUST move finished phases to `.claude/ROADMAP-ARCHIVE.md`.
- After plan approval: implement → build → commit → present next phase plan. MUST NOT stop between phases unless user requests a pause.

## Commit Discipline

- MUST run `/review-phase` before committing a completed implementation phase.
- MUST create one atomic commit per phase. Message MUST describe the phase purpose, not individual files.
- MUST push after each phase so progress is preserved.

## Post-Phase Reflection

After each phase commit, MUST run a lightweight reflection:
- MUST check for patterns worth codifying (repeated solutions, new conventions).
- MUST note operational insights (wasted cycles, effective techniques).
- MUST flag any rule violations encountered during the phase.
- For substantial findings, MUST invoke `/reflect` for full analysis and file updates.

## Related Commands

- For targeted file reviews, MUST use `/review-controller` or `/review-entity`
