Sync code patterns between the dotnet web app template and dotnet projects.

## Arguments

$ARGUMENTS — optional direction and file filter.

Parse as: `[to-template] [file-pattern] [--all-projects]`

If no arguments are provided, use `AskUserQuestion` to offer:
1. **Show recent template changes** — run `git log --oneline -10` and `git diff` on the template repo to show what changed recently
2. **Compare specific files** — ask the user which files to compare (template-relative, using `ProjectName` placeholder)
3. **Backport to template** — ask the user which project file to push back to the template

## Configuration

### Template location
- Windows: `%USERPROFILE%\My Drive\Source\Solhigson\dotnet-web-app-template\src`
- macOS: `~/My Drive/Source/Solhigson/dotnet-web-app-template/src`

### Project name detection
Find the `*.slnx` or `*.sln` file in the project's `src/` directory (or project root if no `src/`). The filename without extension is the project name.

### Config repo location
- Windows: `%USERPROFILE%\My Drive\claude-settings`
- macOS: `~/My Drive/claude-settings`

## Modes

### Compare (default): `/sync-template <file-pattern>`

1. Resolve `<file-pattern>` against the template's file tree. The pattern uses `ProjectName` as placeholder.
   - Filename patterns (e.g. `SomeFile.cs`) → searches the template tree for matches
   - Glob patterns (e.g. `*.csproj`) → finds all matching files in the template
   - Full template-relative paths (e.g. `ProjectName.Domain/ProjectName.Domain.csproj`) → exact match
2. For each matched template file:
   a. Read the template file, replace `ProjectName` with the detected project name in content
   b. Compute the project path by replacing `ProjectName` in the file path
   c. Read the project's corresponding file
   d. Compare and report differences
3. Present a summary: which files match, which differ (with brief description of what's different), which are missing in the project.
4. Wait for the user to say which changes to apply.

### Compare all projects: `/sync-template <file-pattern> --all-projects`

Same as compare, but repeat for every dotnet project:
1. Read `setup.ps1` (Windows) or `setup.sh` (macOS) from the config repo to find all `Link-Project`/`link_project` calls.
2. For each project, check if its `stacks.conf` contains `dotnet`.
3. Run the compare against each dotnet project, reporting results per project.
4. Skip the current project directory if already included.

### Recent changes: `/sync-template --recent`

1. `cd` to the template repo
2. Run `git log --oneline -10` to show recent commits
3. Run `git diff HEAD~1` (or appropriate range) to show what files changed
4. Present the results and ask the user which changes to sync

### Backport to template: `/sync-template to-template <file-path>`

1. `<file-path>` is a project-relative path (e.g., `{ProjectName}.Application/Web/SomeFile.cs`)
2. Read the project file
3. Replace the detected project name with `ProjectName` in the file content
4. Read the current template counterpart (path also substituted)
5. Show the diff: what would change in the template
6. Wait for user confirmation before writing to the template

## Rules

- **Report-only by default** — never apply changes without the user explicitly asking
- **Skip `.generated.cs` files** — never compare or sync generated code
- **Skip `bin/`, `obj/`, `.vs/`, `.idea/`** directories
- **csproj files need surgical edits** — projects have different packages, references, and build targets. When applying csproj changes, use targeted edits (add/remove specific elements), never full-file replacement
- **`ProjectName` substitution is case-sensitive literal text replacement** — replace the exact string `ProjectName`
- **Extra web projects** — if the project has web projects not in the template, mention them when syncing web-project patterns so the user can decide whether to apply there too
