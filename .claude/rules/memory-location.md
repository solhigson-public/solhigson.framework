# Memory File Location

Store all Claude auto-memory files (MEMORY.md and topic files) inside the project's `.claude/memory/` directory, not in the default `~/.claude/projects/` path.

This keeps memory self-contained with the project and synced back to the claude-settings repo via the existing sync-back rule.

**Sync path:** `.claude/memory/*.md` → `<config-repo>/projects/<project-name>/.claude/memory/`
