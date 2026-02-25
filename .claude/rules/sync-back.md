After modifying any Claude-related file in the working project folder, immediately sync the change back to the central config repo and commit.

**Config repo location:** `~/My Drive/claude-settings` (Windows: `%USERPROFILE%\My Drive\claude-settings`, macOS: `~/Library/CloudStorage/GoogleDrive-*/My Drive/claude-settings` or `~/My Drive/claude-settings`).

**Project name:** Infer from the setup scripts — find the `link_project` line whose path matches the current working directory. The first argument contains `projects/<project-name>`.

## What to Sync Where

### 1. CLAUDE.md and reference docs
- `.claude/CLAUDE.md` → `<config-repo>/projects/<project-name>/CLAUDE.md`
- `.claude/*.md` reference docs (e.g. `plan.md`, `PROTOTYPE-PAGES.md`) → `<config-repo>/projects/<project-name>/`

### 2. Rules — stack-aware routing
When a rule file in `.claude/rules/` is modified:

1. Read the project's `stacks.conf` from `<config-repo>/projects/<project-name>/stacks.conf`.
2. Build the stack search order: `common`, then each stack listed in `stacks.conf`.
3. **`.overrides.md` files** → always sync to `<config-repo>/projects/<project-name>/.claude/rules/`. These are always project-specific and never belong in stacks.
4. **Other rule files** → check if the filename exists in any stack's rules directory (`<config-repo>/stacks/<stack>/rules/<filename>`):
   - If found: sync back to that stack location. This benefits all projects using the same stack.
   - If NOT found in any stack: sync to `<config-repo>/projects/<project-name>/.claude/rules/` as a project-specific rule.

### 3. Commands
Same routing logic as rules:
- Check stacks first (common, then declared stacks in order).
- If the file originated from a stack, sync back to the stack.
- If project-specific, sync to `<config-repo>/projects/<project-name>/.claude/commands/`.

### 4. Skills
Same routing logic:
- Check stacks for matching skill directory/files.
- If from a stack, sync back to the stack.
- If project-specific, sync to `<config-repo>/projects/<project-name>/.claude/skills/`.

## How to Sync

1. Copy the modified file to the determined path in the config repo.
2. `cd` to the config repo, `git add -A`, and commit with message: `Sync <project-name>: <brief description of what changed>`.
3. Attempt `git push`. If it fails (no remote, auth issue, etc.), continue silently — the local commit is sufficient.

## Stack Files are Shared

If you sync a rule/command back to a stack, it affects ALL projects that use that stack. Confirm with the user before making changes to stack-level files if the change might be project-specific rather than universal.
