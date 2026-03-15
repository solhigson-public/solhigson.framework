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

### Phase 2 — Framework: Newtonsoft Removal (Non-API) ✅
> Migrate non-API files: `ApiTraceData.cs` (JObject → Dictionary), `ExceptionInfo.cs`, `SolhigsonPermission.cs`, `PagedList.cs`, `XUnitTestOutputHelperTarget.cs`, `ApiRequestService.cs`. Pipeline flag flip to push Utilities 10.1.0 to NuGet. Bump Framework Utilities ref to 10.1.0.

- [x] Plan
- [x] Implement
- [x] Build
- [x] Commit

### Phase 2t — Framework: Newtonsoft Removal Tests ✅
> ApiTraceData dictionary headers + GetUserIdentity, PagedList camelCase GetMetaData + pagination logic, ExceptionInfo STJ serialization. 24 tests.

- [x] Plan
- [x] Write tests (24 tests — ApiTraceData, PagedList, ExceptionInfo)
- [x] Build
- [x] Commit

### Phase 3 — ApiRequestService Redesign ✅
> Consolidated into single `ApiRequest` class (static factory + fluent builder + properties). Redesigned `IApiRequestService` to 2 methods (`SendAsync`, `SendAsync<T>`) with CancellationToken. Rewrote `ApiRequestService`: linked CTS for timeout (thread-safe), sync trace logging, `using` on HttpRequestMessage, constructor-injected `ApiConfiguration`, ExpectContinue default false. Extracted `ContentTypes` constants.

- [x] Plan
- [x] Implement
- [x] Build
- [x] Commit

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
