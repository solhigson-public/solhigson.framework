---
description: Sync code patterns between the dotnet web app template and dotnet projects.
wiring: standalone
---

Sync code patterns between the dotnet web app template and dotnet projects. For configuration paths, detailed mode steps, and sync rules, MUST invoke the `sync-template` skill.

## Arguments

$ARGUMENTS — optional direction and file filter.

Parse as: `[to-template] [file-pattern] [--all-projects]`

If no arguments provided, MUST offer: (1) show recent template changes, (2) compare specific files, (3) backport to template.

## Modes

- **Compare** (default): `/sync-template <file-pattern>` — resolve pattern against template, compare with project, report diffs. MUST wait for user before applying.
- **Compare all**: `/sync-template <file-pattern> --all-projects` — repeat compare for every dotnet project found in setup scripts.
- **Recent**: `/sync-template --recent` — show last 10 template commits and diffs.
- **Backport**: `/sync-template to-template <file-path>` — push project file back to template with `ProjectName` substitution. MUST wait for user confirmation.

See skill reference for detailed steps per mode.

## Rules

- **Report-only by default** — MUST NEVER apply changes without explicit user request
- **MUST skip `.generated.cs`** files and `bin/`, `obj/`, `.vs/`, `.idea/` directories
- **csproj files MUST use surgical edits** — MUST NEVER use full-file replacement
- **`ProjectName` substitution is case-sensitive literal** — MUST replace exact string `ProjectName`
- **Extra web projects** — MUST mention project web projects not in template so user can decide
