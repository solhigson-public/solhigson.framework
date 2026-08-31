# Releasing Solhigson Packages

Consumers (all dotnet projects) reference **Solhigson.Framework.Core** only; **Solhigson.Utilities** flows into them transitively through Framework.Core's own `<PackageReference Include="Solhigson.Utilities" ...>`. **Solhigson.Framework.EfCoreTool** is a separate build-time tool package. Consumer repos work this framework via sibling checkout: every repo on a machine lives in that machine's shared Source folder, so the framework resolves relative to the consumer repo root (a sibling named `solhigson.framework` or `solhigson.framework.core` depending on the machine); the absolute prefix varies per machine and is never hardcoded.

## How publishing works

- Azure Pipelines (`azure-pipelines.yml`, repo root) publishes to nuget.org automatically on a push to `master`.
- Each package has its own pipeline stage gated by a variable toggle: `packageFramework`, `packageUtilities`, `packageTool`. A toggle is armed by flipping the third operand of its `and(eq(variables.package, true), eq(variables.isMaster, true), <bool>)` expression to `true`.
- Stages pack with `versioningScheme: 'off'`: the csproj `PackageVersion` alone decides the published version (`Version` and `PackageId` stay untouched).
- nuget.org refuses duplicate versions: a master push with a toggle armed but no version bump fails that stage's push step. Noise, not harm, but check the toggle state before any unrelated master push.

## Release procedure

1. **One release commit per package**: bump the csproj `PackageVersion` AND arm that package's toggle in the SAME commit (precedent: `a3dbab2`, `8f8fb0b`, `2bc0a6f`). Commit subject: `Bump <package> PackageVersion to X.Y.Z`, body listing the changes on master since the last release (precedent: `57a5969`).
2. **Dependency order**: release `Solhigson.Utilities` FIRST and confirm the new version is restorable on nuget.org BEFORE pushing the `Solhigson.Framework.Core` commit that references it, or the Framework build fails restore (NU1102).
3. **Disarm after**: once the package is live, a follow-up commit flips the toggle back to `false` (precedent: `3f8b27b`, `dac505e`, `91572ac`). Resting state: all three toggles disarmed.
4. **Consumers**: bump the `Solhigson.Framework.Core` entry in each consumer's `Directory.Packages.props` once the new Framework.Core is restorable.
5. **Bump once per completed body of work**, never per change unit: a multi-CU campaign gets a single release at its end.
