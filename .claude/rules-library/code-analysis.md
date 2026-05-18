---
name: Code Analysis
description: "Static analysis policy — zero-diagnostic requirement on touched files, opt-out suppression model with justification comments"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
---

# Code Analysis

Every project MUST use its stack's standard static analysis tooling with strict defaults. MUST use an **opt-out model**: all diagnostic categories enabled at `warning` severity by default. Individual rules MUST only be suppressed with a justification comment.

## Rules

- MUST produce zero diagnostics (warnings, suggestions) on touched files before committing
- New diagnostics from tooling upgrades MUST auto-apply — no manual tracking
- Suppressions MUST include a justification comment explaining why
- MUST NOT blanket-suppress a diagnostic category — MUST suppress individual rules only
