Onboard the current project into the claude-settings configuration repo.

## 0. Guard — Reject if run from claude-settings

Check if the current working directory is inside the claude-settings config repo (e.g. `C:\Users\eawag\My Drive\claude-settings` or its platform equivalent). If so, **stop immediately** and print:

> This command must be run from the target project folder, not from the claude-settings repo itself.

Do not proceed further.

## 1. Check for Existing Code

Scan the current directory for source code files (`.cs`, `.csproj`, `.sln`, `.slnx`, `.dart`, `.js`, `.ts`, `.py`, `.go`, `.java`, `.rs`, etc.).

- **If code exists** — infer the project name from the folder name (lowercase, e.g. `my-flutter-app`). Present the inferred name and ask the user to confirm or provide an alternative. Skip to **Step 3** (Select Stacks).
- **If the directory is empty or has no source code** — proceed to **Step 2** (Scaffold).

## 2. Scaffold New Project (empty directory only)

### 2a. Ask for project name

Ask the user: **"What is the project/solution name?"** (PascalCase, e.g. `MyApp`, `Elfrique`)

This name is used for namespaces, folder prefixes, and the solution file. Derive the **config repo name** automatically by lowercasing (e.g. `MyApp` -> `myapp`, `Elfrique` -> `elfrique`). Present both names and ask the user to confirm.

### 2b. Ask for stack

Ask: **"Which stack should this project use?"**

List available stacks by scanning `<config-repo>/stacks/` directories (excluding `common`). Include an option for "none".

### 2c. Scaffold from template (dotnet only)

If the user selected `dotnet`:

1. Copy the entire `src/` directory from `C:\Users\eawag\My Drive\Source\Solhigson\dotnet-web-app-template` into the current working directory.
2. Rename all **folders** containing `ProjectName` to use the chosen project name (e.g. `ProjectName.Domain` -> `MyApp.Domain`).
3. Rename all **files** containing `ProjectName` to use the chosen project name (e.g. `ProjectName.slnx` -> `MyApp.slnx`).
4. **Find-and-replace** the literal string `ProjectName` with the chosen project name in **all file contents**. This includes namespaces, project references, constants, build targets, and generated files.
5. Leave the `SourceGenerators/` directory untouched — it is not prefixed with `ProjectName`.
6. Run `dotnet restore` in the `src/` directory to verify the solution is valid.

If the user selected a stack other than `dotnet`, print: "No template available for this stack. Continuing with an empty project." and proceed.

**The stack is now known — skip the stack selection prompt in Step 3.**

## 3. Select Stacks (existing code only)

This step runs only if code already existed in Step 1 (stack was not chosen in Step 2).

Ask the user: **"Which stacks should this project use?"**

List available stacks by scanning `<config-repo>/stacks/` directories (excluding `common`, which is always included automatically).

Example prompt:
```
Available stacks (common is always included):
  - dotnet
  - flutter

Which stacks? (comma-separated, or "none" for common only):
```

## 4. Write stacks.conf

Write the selected stack names to `<config-repo>/projects/<project-name>/stacks.conf`, one per line. Do NOT include `common` in the file — it is always implicit.

## 5. Scan Codebase

If the directory was scaffolded (Step 2) or already had code (Step 1), ask:

**"Should I scan the codebase to generate a project-specific CLAUDE.md?"**

- If **yes** — analyse the source code: architecture, folder structure, patterns, conventions, frameworks, naming, build system, existing CLAUDE.md or .claude/ config. Then generate a project CLAUDE.md from the findings. **Important: deduplicate against the global `~/.claude/CLAUDE.md` AND all selected stack rules** — the project file must only contain project-specific content (project info, solution structure, business domain, specific services/jobs/integrations, constants). Do NOT repeat patterns already covered globally or in stacks. All files are loaded together in-context.
- If **no** — create a minimal CLAUDE.md stub with just the project name and path, to be filled in later by the user.

## 6. Create Project Config

Create the following in the config repo at `projects/<project-name>/`:

### stacks.conf
Already created in Step 4.

### CLAUDE.md
Either the full generated version (if scanned) or the minimal stub.

### .claude/rules/ (if applicable)
Only if codebase was scanned and strong project-specific patterns were found. Do **not** duplicate rules that already exist in any selected stack. Use `{name}.overrides.md` for exceptions to stack rules, or `{name}.md` for purely additive project rules.

## 7. Update Setup Scripts

Add the project mapping to **both** setup scripts:

**In `setup.sh`** (before the "Add more projects" comment):
```bash
NEWPROJECT_DIR="<current-working-directory>"
link_project "projects/<project-name>" "$NEWPROJECT_DIR"
```

**In `setup.ps1`** (before the "Add more projects" comment):
```powershell
Link-Project "projects\<project-name>" "<current-working-directory>"
```

Note: No separate `Link-SharedRules`, `Link-SharedCommands`, or `Link-SharedSkills` calls needed — `Link-Project` handles stacks automatically via `stacks.conf`.

## 8. Deploy Files

Run the copy/symlink deployment for this project:
- Deploy common stack (always): rules, commands to `.claude/`
- Deploy each declared stack: rules, commands, skills to `.claude/`
- Copy CLAUDE.md to `.claude/CLAUDE.md`
- Copy reference docs to `.claude/`
- Copy project-specific `.claude/rules/*.md` (if any)
- Create `.claude/sessions/archive/` directory

## 9. Commit

Stage and commit all new files to the claude-settings repo:
```
git add -A && git commit -m "Add <project-name> project configuration"
```

## 10. Summary

Print a summary of:
- What was created in the config repo
- What was deployed to the project folder
- Which stacks were selected
- Whether the project was scaffolded from a template or already had code
- Whether a codebase scan was performed or skipped
