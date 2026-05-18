---
name: Sync Template
description: "Sync code patterns between the dotnet web app template repo and project implementations — bidirectional pattern propagation"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - managing sessions
depends_on: []
---

# Sync Template Skill

Supports the `/sync-template` command with detailed mode descriptions, configuration, and rules.

## Files

- `reference.md` — mode details (compare, compare-all, recent, backport), configuration paths, and sync rules
