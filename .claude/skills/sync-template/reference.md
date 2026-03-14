# Sync Template — Reference

## Configuration

### Template location
- Windows: `%USERPROFILE%\My Drive\Source\Solhigson\dotnet-web-app-template\src`
- macOS: `~/My Drive/Source/Solhigson/dotnet-web-app-template/src`

### Project name detection
MUST find `*.slnx` or `*.sln` in project's `src/` directory (or project root if no `src/`). Filename without extension is the project name.

### Config repo location
- Windows: `%USERPROFILE%\My Drive\claude-settings`
- macOS: `~/My Drive/claude-settings`

## Mode Details

### Compare (default): `/sync-template <file-pattern>`

1. MUST resolve `<file-pattern>` against template file tree. Pattern uses `ProjectName` as placeholder.
   - Filename patterns (e.g. `SomeFile.cs`) -> MUST search template tree for matches
   - Glob patterns (e.g. `*.csproj`) -> MUST find all matching files
   - Full template-relative paths (e.g. `ProjectName.Domain/ProjectName.Domain.csproj`) -> exact match
2. For each matched template file:
   a. MUST read template file, replace `ProjectName` with detected project name
   b. MUST compute project path by replacing `ProjectName` in file path
   c. MUST read project's corresponding file
   d. MUST compare and report differences
3. MUST present summary: files that match, differ (with brief diff description), or are missing in project.
4. MUST wait for user to say which changes to apply.

### Compare all projects: `/sync-template <file-pattern> --all-projects`

Same as compare, but MUST repeat for every dotnet project:
1. MUST read `setup.ps1` (Windows) or `setup.sh` (macOS) from config repo to find all project calls.
2. For each project, MUST check if its `stacks.conf` contains `dotnet`.
3. MUST run compare against each dotnet project, reporting per project.
4. MUST skip current project directory if already included.

### Recent changes: `/sync-template --recent`

1. MUST `cd` to template repo
2. MUST run `git log --oneline -10` and `git diff HEAD~1` to show recent changes
3. MUST present results and ask user which changes to sync

### Backport to template: `/sync-template to-template <file-path>`

1. `<file-path>` is project-relative (e.g., `{ProjectName}.Application/Web/SomeFile.cs`)
2. MUST read project file
3. MUST replace detected project name with `ProjectName` in content
4. MUST read current template counterpart (path also substituted)
5. MUST show diff: what would change in template
6. MUST wait for user confirmation before writing

## Rules

- **Report-only by default** — MUST NEVER apply changes without explicit user request
- **MUST skip `.generated.cs` files** — MUST NEVER compare or sync generated code
- **MUST skip `bin/`, `obj/`, `.vs/`, `.idea/`** directories
- **csproj files MUST use surgical edits** — projects have different packages/references/build targets. MUST use targeted edits, MUST NEVER use full-file replacement
- **`ProjectName` substitution is case-sensitive literal text replacement** — MUST replace exact string `ProjectName`
- **Extra web projects** — if project has web projects not in template, MUST mention them when syncing web-project patterns
