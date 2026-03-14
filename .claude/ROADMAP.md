# Solhigson Framework Implementation Roadmap

Each phase follows: **Plan → Implement → Build → Commit**
Each test phase follows: **Plan → Write Tests → Build → Commit**

---

## v10.1.0 — Remove Newtonsoft.Json, Redesign ApiRequestService, Remove Obsolete Members

> Source: `.claude/plans/newtonsoft-removal-api-redesign.md` (reference material — scope, design decisions, interface changes)
> Breaking change release. Tied to .NET 10 version scheme.

### Phase 1 — Utilities: Newtonsoft Removal ✅
> Rewrite `Serializer.cs` (Newtonsoft → STJ), `HelperFunctions.cs` (JObject → Dictionary/JsonNode), clean `ResponseInfo.cs` dual attributes, remove Newtonsoft NuGet, bump Utilities to 10.1.0.

- [x] Plan
- [x] Implement
- [x] Build
- [x] Commit

### Phase 1t — Utilities: Serialization Tests ✅
> Set up test infrastructure (TestBase, xUnit conventions) + verify STJ behavioral compatibility — camelCase serialization, null handling, reference loop ignoring, key-value flattening, protected field masking.

- [x] Plan
- [x] Write tests (34 tests — serializer, protected fields, ResponseInfo)
- [x] Build
- [x] Commit

### Phase 2 — Framework: Newtonsoft Removal (Non-API)
> Migrate non-API files: `ApiTraceData.cs` (JObject → Dictionary), `ExceptionInfo.cs`, `SolhigsonPermission.cs`, `PagedList.cs`, `XUnitTestOutputHelperTarget.cs`. Fix callers of changed return types in middleware and caching.

- [ ] Plan
- [ ] Implement
- [ ] Build
- [ ] Commit

### Phase 2t — Framework: Newtonsoft Removal Tests
> Attribute serialization, protected field masking with JsonNode, trace data dictionary conversion, middleware integration.

- [ ] Plan
- [ ] Write tests
- [ ] Build
- [ ] Commit

### Phase 3 — ApiRequestService Redesign
> Create `ApiRequest` builder (static factory + fluent chain). Redesign `IApiRequestService` to 2 methods (`SendAsync`, `SendAsync<T>`). Rewrite `ApiRequestService`: add CancellationToken, STJ deserialization, remove Task.Run(), remove exception swallowing, add `using` on HttpRequestMessage, ExpectContinue default false.

- [ ] Plan
- [ ] Implement
- [ ] Build
- [ ] Commit

### Phase 3t — ApiRequestService Tests
> Builder construction, SendAsync with CT propagation, trace logging, error classification, named client resolution.

- [ ] Plan
- [ ] Write tests
- [ ] Build
- [ ] Commit

### Phase 4 — Obsolete Removal, Version Bump & Final Verification
> Delete 7 `ELog*` methods, 2 `RepositoryBase.Get()` overloads, 5 `PermissionAttribute` properties. Bump Framework to 10.1.0. Grep audit: zero Newtonsoft references. Full `dotnet test`.

- [ ] Plan
- [ ] Implement
- [ ] Build
- [ ] Commit

### Phase 4t — Final Test Suite Validation
> Full `dotnet test` pass, verify no test regressions from obsolete member removal, integration validation across both packages.

- [ ] Plan
- [ ] Write tests
- [ ] Build
- [ ] Commit

---

## Dependency Graph

```
Phase 1 (Utilities) → Phase 1t → Phase 2 (Framework non-API) → Phase 2t
    → Phase 3 (API Redesign) → Phase 3t → Phase 4 (Cleanup + Version) → Phase 4t
        → NuGet Publish → Elfrique consumer update (tracked in Elfrique roadmap)
```
