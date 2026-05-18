---
name: Scan Drift
description: "Grep-accelerated compliance scan — mechanically verify code against all loaded rules using pattern-based detection"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - auditing code
  - reviewing rules
depends_on: []
---

# Scan Drift — Grep Accelerators

Grep patterns for rules that can be mechanically verified. Used by `/scan-drift` as a reference for the Task agent during deep scans.

## When This Skill Is Invoked
- During `/scan-drift` deep scan for fast pattern matching
- When reviewing a PR for quick pattern checks

MUST follow `reference.md` for grep patterns and analysis logic. If `reference.md` does not exist, MUST stop and report the missing file to the user.
