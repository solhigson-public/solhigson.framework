# Codifying Patterns and Conventions

When the user decides to codify a pattern, style, convention, or workflow rule during a session:

1. **Determine placement** — decide whether it belongs in:
   - `CLAUDE.md` (project overview, high-level context)
   - `rules/` (enforceable conventions, coding standards)
   - `skills/` (reusable multi-step workflows)
   - `commands/` (slash-command procedures)

2. **Determine scope** — ask the user:
   - **Project-specific**: only applies to the current project
   - **Stack-level**: applies to all projects using the same tech stack (e.g., all dotnet projects)
   - **Global**: applies to all projects regardless of stack

3. **Handle conflicts** — if the new rule relates to an existing stack or global rule:
   - If it's an **exception** to a stack rule → create `{rule-name}.overrides.md` in the project's `.claude/rules/`
   - If it's **additive** (no conflict) → create a uniquely named file at the appropriate scope
   - If it **modifies** a stack rule for all projects using that stack → update the stack rule directly (confirm first since it affects all projects)

4. **Confirm before writing** — always confirm the proposed file name, location, and scope with the user before creating or modifying any config file.

5. **Sync back** — after writing, follow the sync-back rule to commit changes to the config repo.

6. **Deploy on shared changes** — if the change is stack-level or global (not project-specific), run the setup script after committing to deploy the update to all projects.
