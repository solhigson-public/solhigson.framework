# Continuation Prompt

<!-- STATUS: PENDING -->

## Mandate
Fix two remaining findings in PermissionManager.cs from the full file review, verify prior fixes are staged, then commit all changes.

## Context Files
- Session: `docs/sessions/2026-04-07-permissionmanager-idempotency-review.md`
- Tracker: N/A
- Cumulative: N/A

## Execution Steps
1. Run `git diff --cached -- src/Solhigson.Framework/Identity/PermissionManager.cs` and verify ALL three prior-session fixes are staged:
   - (a) `permissionList.TryAdd(solhigsonPermission.Name, solhigsonPermission)` appears in the custom permissions loop (was `Add`)
   - (b) No commented-out `// if (permissionList.ContainsKey` or `// if (await _dbContext.Permissions.AnyAsync` blocks remain in `DiscoverNewPermissionsAsync`
   - (c) A `var existingPermissionNames = ...ToHashSet()` line exists before the seeding foreach loop
   - If any of (a), (b), or (c) are missing, MUST apply them before proceeding to Step 2
2. Read `src/Solhigson.Framework/Identity/PermissionManager.cs` to locate the exact line numbers for Steps 3-5
3. Fix stale HashSet (review finding F03): Inside the `if (!existingPermissionNames.Contains(permission.Name))` block in `DiscoverNewPermissionsAsync`, immediately after the `count++;` line, add: `existingPermissionNames.Add(permission.Name);`
4. Fix awkward LINQ (review finding F07): Replace `foreach (var permission in from key in permissionList.Keys select permissionList[key])` with `foreach (var permission in permissionList.Values)`
5. In `DiscoverNewPermissionsAsync`, verify `permissionList.Add(permission.Name, permission)` (the second Add call, in the controller-attribute discovery loop) uses `TryAdd`. If it still uses `Add`, change it to `TryAdd`
6. Stage changes: `git add src/Solhigson.Framework/Identity/PermissionManager.cs`
7. Run `dotnet build src/Solhigson.Framework.sln --configuration Release --nologo -v q` — if the build produces errors, MUST fix the introduced errors and re-run. MUST NOT proceed to Step 8 with build errors
8. Commit (new commit, NOT --amend) with message: "fix: make permission seeding idempotent and clean up PermissionManager" — this commit completes the review-driven fixes that build on b880a81

## Acceptance Criteria
- `permissionList.TryAdd` used in BOTH places where permissions are added to the dictionary (custom permissions loop AND controller-attribute discovery loop)
- No commented-out code blocks remain in `DiscoverNewPermissionsAsync`
- Single bulk query loads existing permission names into HashSet before the seeding loop
- `existingPermissionNames.Add(permission.Name)` called after `count++` inside the insert block
- Seeding loop uses `permissionList.Values` instead of LINQ query syntax over Keys
- Solution builds with 0 errors
- All changes in a single new commit (not an amend)

## Anti-Instructions
- MUST NOT modify any file other than `src/Solhigson.Framework/Identity/PermissionManager.cs`
- MUST NOT add CancellationToken parameters (deferred to separate work)
- MUST NOT refactor the per-item SaveChangesAsync pattern (deferred to separate work)
- MUST NOT add constructor validation (deferred to separate work)
- MUST NOT change method signatures or public API surface
- MUST NOT use `git commit --amend`
