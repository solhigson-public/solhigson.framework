---
description: Deep compliance scan — verify code conforms to all loaded rules.
---

Verify the codebase conforms to all loaded rules. MUST delegate to a Task agent for unbiased review (fresh context, no authoring bias). For grep-accelerator patterns and mechanical checks, MUST invoke the `scan-drift` skill.

## Procedure

1. MUST determine scope — default is full codebase. User may specify files, directories, or a phase.
2. MUST delegate the scan to a Task agent.
3. The agent MUST:
   a. Read all rule files from `.claude/rules/`.
   b. Read each code file in scope.
   c. Check each file against all applicable rules — rules are self-describing, no pre-mapped lookups.
   d. Report violations with: rule name, file path, line number, what's wrong.
4. MUST group findings by severity:
   - **Critical**: security risks (missing auth, layer violations, exposed secrets)
   - **Warning**: pattern deviations (return types, facade candidates, naming)
   - **Info**: minor convention deviations
