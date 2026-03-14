# Dotnet Project Scaffold

Scaffold a new .NET project from the template. Called by the `new-project` command (Step 2c).

## Prerequisites

- Project name (PascalCase) determined in Step 2a.
- Template source: `C:\Users\eawag\My Drive\Source\Solhigson\dotnet-web-app-template`

## Steps

1. Copy the entire `src/` directory from the template into the current working directory.
2. Rename all **folders** containing `ProjectName` to use the chosen project name (e.g. `ProjectName.Domain` -> `MyApp.Domain`).
3. Rename all **files** containing `ProjectName` to use the chosen project name (e.g. `ProjectName.slnx` -> `MyApp.slnx`).
4. **Find-and-replace** the literal string `ProjectName` with the chosen project name in **all file contents**. This includes namespaces, project references, constants, build targets, and generated files.
5. Leave the `SourceGenerators/` directory untouched — it is not prefixed with `ProjectName`.
6. Run `dotnet restore` in the `src/` directory to verify the solution is valid.
