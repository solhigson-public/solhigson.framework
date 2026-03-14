# Plan: Remove Newtonsoft.Json, Redesign ApiRequestService, Remove Obsolete Members

## Context

The Solhigson framework (`Solhigson.Framework.Core` 10.0.17 + `Solhigson.Utilities` 10.0.1) depends on `Newtonsoft.Json` 13.0.4 throughout both packages. `System.Text.Json` (STJ) is already referenced and used alongside Newtonsoft in several files (dual `[JsonIgnore]` attributes on `ResponseInfo`, dual `[JsonProperty]`/`[JsonPropertyName]` on `ExceptionInfo`). This creates:

1. **Two serialization stacks** — consumers must understand which serializer each code path uses
2. **Attribute confusion** — some types need dual attributes, API model types in consumers can only use one
3. **`ApiRequestService` design debt** — 15+ overloads with 7-8 optional parameters, no `CancellationToken`, `Task.Run()` for trace logging, silent exception swallowing, double-allocation response deserialization
4. **Accumulated `[Obsolete]` members** — `ELog*` logger extensions, `RepositoryBase.Get()`, `PermissionAttribute` menu properties — all have replacements in active use

This is a breaking change release: remove Newtonsoft entirely, redesign the `IApiRequestService` interface, and clean up obsolete members. Version bump to `10.1.0` / `10.1.0` (tied to .NET version).

## Scope

### Packages affected
- `Solhigson.Utilities` (10.0.1 → 10.1.0)
- `Solhigson.Framework.Core` (10.0.17 → 10.1.0)

### Files to modify — Solhigson.Utilities
- `Solhigson.Utilities.csproj` — remove `Newtonsoft.Json` package ref, bump version
- `Serializer.cs` — rewrite `SerializeToJson`/`DeserializeFromJson` from Newtonsoft to STJ; rewrite `SerializeToKeyValue` from `JObject`/`JToken` to `JsonNode`
- `HelperFunctions.cs` — `ToJsonObject()` overloads: `JObject` → `Dictionary<string, string>`; `CheckForProtectedFields`/`MaskProtectedProperties`: `JObject`/`JToken`/`JProperty`/`JArray` → `JsonNode`/`JsonObject`/`JsonArray`/`JsonValue`
- `Dto/ResponseInfo.cs` — remove `Newtonsoft.Json.JsonIgnore` attributes (keep STJ `JsonIgnore` only)

### Files to modify — Solhigson.Framework
- `Solhigson.Framework.csproj` — bump version
- `Web/Api/IApiRequestService.cs` — redesign: 2 methods + builder pattern (remove all `GetData*`/`PostData*` convenience methods)
- `Web/Api/ApiRequestService.cs` — rewrite: add `CancellationToken`, stream-based deserialization, remove `Task.Run()` for trace logging, remove outer exception swallowing, add `using` on `HttpRequestMessage`, change `ExpectContinue` default to `false`
- `Web/Api/ApiRequestDetails.cs` — add fluent builder API (`ApiRequest.Get(uri).WithJsonBody().WithHeader().Build()`)
- `Web/Api/ApiRequestResponse.cs` — no Newtonsoft changes needed (no Newtonsoft usage)
- `Logging/ApiTraceData.cs` — `JObject` properties → `Dictionary<string, string>`; `[JsonIgnore]` Newtonsoft → STJ
- `Logging/ExceptionInfo.cs` — remove dual `[JsonProperty]`/`[JsonPropertyName]` → STJ only
- `Logging/Nlog/Targets/XUnitTestOutputHelperTarget.cs` — `JToken.Parse()` → `JsonNode.Parse()` or `JsonDocument.Parse()`
- `Data/PagedList.cs` — `[JsonProperty]` Newtonsoft → `[JsonPropertyName]` STJ (or remove if unnecessary)
- `Identity/SolhigsonPermission.cs` — `[Newtonsoft.Json.JsonIgnore]` → `[System.Text.Json.Serialization.JsonIgnore]`
- `Extensions/LoggerExtensions.cs` — delete 7 `[Obsolete]` `ELog*` methods
- `Data/Repository/RepositoryBase.cs` — delete 2 `[Obsolete]` `Get()` overloads
- `Web/Attributes/PermissionAttribute.cs` — delete 5 `[Obsolete]` properties (`Description`, `IsMenuRoot`, `IsMenu`, `MenuIndex`, `Icon`)
- `Web/Middleware/ApiTraceMiddleware.cs` — update `ToJsonObject()` calls (return type changes from `JObject` to `Dictionary<string, string>`)
- `Web/SolhigsonMvcControllerBase.cs` — update `DeserializeFromJson`/`SerializeToJson` calls if signature changes
- `EfCore/Caching/MemoryCacheProvider.cs` — update serialization calls
- `EfCore/Caching/RedisCacheProvider.cs` — update serialization calls
- `Web/Middleware/PermissionsMiddleware.cs` — update serialization calls
- `Web/Middleware/ExceptionHandlingMiddleware.cs` — update serialization calls

### Files to modify — Elfrique (consumer, after framework publish)
- `src/Directory.Packages.props` — bump Solhigson package versions to 10.1.0
- `src/Elfrique.Application/Services/PaymentProviders/PaystackPaymentProvider.cs` — refactor to use `IApiRequestService`
- `src/Elfrique.Application/Services/PaymentProviders/FlutterwavePaymentProvider.cs` — refactor to use `IApiRequestService`
- `src/Elfrique.Application/Services/TravelProviders/TiqwaFlightProvider.cs` — refactor to use `IApiRequestService`
- `src/Elfrique.Application/Services/PaymentProviders/KoraPayIdentityService.cs` — refactor to use `IApiRequestService`
- `src/Elfrique.Application/Jobs/NonRecurring/GetIpInfo.cs` — update `GetDataJsonAsync` call to new API
- `.claude/rules/` — codify outbound HTTP rule

### Out of scope
- `Solhigson.Framework.Playground` (`WsdlParser.cs`) — playground code, not packaged
- `Solhigson.Framework.EfCoreTool` — no Newtonsoft dependency
- XML serialization in `Serializer.cs` — no Newtonsoft involvement, unchanged
- Reducing `IApiRequestService` overloads further (e.g., removing XML support) — separate concern
- Migrating Elfrique's existing `SerializeToJson`/`DeserializeFromJson` extension method calls — they'll work with the new STJ-based implementation transparently

## Design Decisions

- **`10.1.0` version** — tied to .NET version (`10.x` = .NET 10), consistent with Microsoft's package versioning for EF Core, ASP.NET Core, etc. Breaking changes within the same .NET major use minor version bumps.

- **Clean break on `IApiRequestService`** — remove all 15+ `GetDataJsonAsync`/`PostDataJsonAsync`/`PostDataXmlAsync` convenience methods. Replace with 2 methods (`SendAsync`, `SendAsync<T>`) plus a fluent builder for `ApiRequestDetails`. Rationale: the overloads differ only in HTTP method + content type, which the builder handles more clearly. Consumers calling removed methods get compile errors with obvious migration path.

- **Fluent builder on `ApiRequestDetails`** — static factory methods (`ApiRequest.Get(uri)`, `ApiRequest.Post(uri, body)`) return a builder that chains `.WithHeader()`, `.WithNamedClient()`, `.WithTimeout()`, `.WithContentType()`, `.LogTrace()`. `Build()` returns `ApiRequestDetails`. Keeps construction readable and eliminates positional parameter confusion.

- **Stream-based deserialization** — replace `ReadAsStringAsync()` → `DeserializeFromJson<T>(string)` with `ReadFromJsonAsync<T>(stream)` using STJ. Eliminates double allocation. The raw response string is still captured for trace logging (read once into string, deserialize from string) — trace logging needs the raw text.

- **`CancellationToken` on `SendAsync`** — propagated to `HttpClient.SendAsync(request, ct)` and `ReadFromJsonAsync<T>(stream, ct)`. Breaking change on the interface — all callers must pass CT (or use default).

- **Remove `Task.Run()` for trace logging** — replace with synchronous structured log call. The trace data is already assembled; the log write is a structured logging call that NLog handles asynchronously via its own async target. No need for `Task.Run()`.

- **`ExpectContinue` default to `false`** — most REST APIs don't use 100 Continue. Removes unnecessary round-trip latency. Opt-in via builder: `.WithExpectContinue()`.

- **`HelperFunctions.ToJsonObject()` → `Dictionary<string, string>`** — `JObject` was used as a key-value container for HTTP headers. `Dictionary<string, string>` is simpler, no serialization library dependency. `ApiTraceData` properties change type accordingly.

- **`Serializer.SerializeToKeyValue`** — rewrite from `JObject.FromObject()`/`JToken` tree walking to `JsonSerializer.SerializeToNode()` + `JsonNode` tree walking.

- **`CheckForProtectedFields` / `MaskProtectedProperties`** — rewrite from `JObject`/`JToken`/`JProperty` to `JsonNode`/`JsonObject`/`JsonArray`. Same masking logic, different tree API.

- **`PagedList<T>` attributes** — `[JsonProperty]` on read-only properties was for Newtonsoft serialization of constructor-initialized records. STJ handles records with `[JsonInclude]` on properties or uses source generators. Evaluate whether attributes are needed at all — STJ serializes public properties by default.

## Interface Changes

### `IApiRequestService` (redesigned)
```csharp
public interface IApiRequestService
{
    Task<ApiRequestResponse> SendAsync(
        ApiRequestDetails request, CancellationToken ct = default);
    Task<ApiRequestResponse<T>> SendAsync<T>(
        ApiRequestDetails request, CancellationToken ct = default);
}
```

### `ApiRequest` (new static builder factory)
```csharp
public static class ApiRequest
{
    public static ApiRequestBuilder Get(string uri);
    public static ApiRequestBuilder Post(string uri, object? body = null);
    public static ApiRequestBuilder Put(string uri, object? body = null);
    public static ApiRequestBuilder Delete(string uri, object? body = null);
}

public class ApiRequestBuilder
{
    public ApiRequestBuilder WithHeader(string key, string value);
    public ApiRequestBuilder WithBearerToken(string token);
    public ApiRequestBuilder WithNamedClient(string clientName);
    public ApiRequestBuilder WithTimeout(int seconds);
    public ApiRequestBuilder WithContentType(string contentType);
    public ApiRequestBuilder WithExpectContinue(bool value = true);
    public ApiRequestBuilder WithLogTrace(bool value = true);
    public ApiRequestBuilder WithServiceName(string name);
    public ApiRequestBuilder WithServiceDescription(string description);
    public ApiRequestBuilder WithHttpContent(HttpContent content);
    public ApiRequestDetails Build();
}
```

### `ApiTraceData` (changed property types)
```csharp
// Before: JObject RequestHeaders, JObject? ResponseHeaders
// After:
public Dictionary<string, string>? RequestHeaders { get; set; }
public Dictionary<string, string>? ResponseHeaders { get; set; }
```

### `HelperFunctions.ToJsonObject()` (changed return types)
```csharp
// Before: JObject
// After:
public static Dictionary<string, string> ToJsonObject(
    IEnumerable<KeyValuePair<string, string>> dictionary);
public static Dictionary<string, string>? ToJsonObject(
    HttpResponseHeaders? headers);
public static Dictionary<string, string>? ToJsonObject(
    IHeaderDictionary? dictionary);
```

### Removed members
- `IApiRequestService`: `GetDataJsonAsync`, `GetDataJsonAsync<T>`, `GetDataXmlAsync`, `GetDataXmlAsync<T>`, `GetDataPlainAsync`, `PostDataJsonAsync`, `PostDataJsonAsync<T>`, `PostDataXmlAsync`, `PostDataXmlAsync<T>`, `PostDataAsync`, `PostDataXWwwFormUrlencodedAsync` (4 overloads), `UseConfiguration`
- `LoggerExtensions`: `ELogTrace`, `ELogDebug`, `ELogInfo`, `ELogWarn`, `ELogError` (2 overloads), `ELogFatal`
- `RepositoryBase<T>`: `Get(Expression)`, `Get<Tk>(Expression)`
- `PermissionAttribute`: `Description`, `IsMenuRoot`, `IsMenu`, `MenuIndex`, `Icon`

## Implementation Steps

### Phase 1: Solhigson.Utilities — Newtonsoft Removal

1. [ ] `Serializer.cs` — rewrite `SerializeToJson` from `JsonConvert.SerializeObject` to `System.Text.Json.JsonSerializer.Serialize`, rewrite `DeserializeFromJson<T>` from `Newtonsoft.JsonSerializer.Deserialize` to `System.Text.Json.JsonSerializer.Deserialize<T>`. Default options: `PropertyNameCaseInsensitive = true`, `PropertyNamingPolicy = CamelCase` (matching current Newtonsoft `CamelCasePropertyNamesContractResolver`), `ReferenceHandler = IgnoreCycles` (matching `ReferenceLoopHandling.Ignore`). Rewrite `SerializeToKeyValue` from `JObject`/`JToken` to `JsonNode` tree.
   - Verify: `Solhigson.Utilities` builds with zero errors, no Newtonsoft `using` statements remain in `Serializer.cs`

2. [ ] `HelperFunctions.cs` — change 3 `ToJsonObject()` overloads to return `Dictionary<string, string>` instead of `JObject`. Rewrite `CheckForProtectedFields` / `MaskProtectedProperties` from `JObject`/`JToken`/`JProperty`/`JArray` to `JsonNode`/`JsonObject`/`JsonArray`/`JsonValue`.
   - Verify: `Solhigson.Utilities` builds with zero errors, no `Newtonsoft.Json.Linq` references remain in `HelperFunctions.cs`

3. [ ] `Dto/ResponseInfo.cs` — remove all `Newtonsoft.Json.JsonIgnore` attributes, keep only `System.Text.Json.Serialization.JsonIgnore`. Remove `using Newtonsoft.Json`.
   - Verify: builds clean

4. [ ] `Solhigson.Utilities.csproj` — remove `<PackageReference Include="Newtonsoft.Json" />`, bump `<PackageVersion>` to `10.1.0`.
   - Verify: `dotnet build` succeeds, no Newtonsoft references in any `.cs` file under `Solhigson.Utilities/`

### Phase 2: Solhigson.Framework — Newtonsoft Removal (Non-API)

5. [ ] `Logging/ApiTraceData.cs` — change `RequestHeaders` and `ResponseHeaders` from `JObject`/`JObject?` to `Dictionary<string, string>`/`Dictionary<string, string>?`. Remove `[JsonIgnore]` Newtonsoft → STJ. Remove `using Newtonsoft.Json` and `using Newtonsoft.Json.Linq`. Update `GetUserIdentity()` to use dictionary lookup instead of `JToken`.
   - Verify: builds clean

6. [ ] `Logging/ExceptionInfo.cs` — remove all `[JsonProperty]` attributes (keep `[JsonPropertyName]` only). Remove `using Newtonsoft.Json`.
   - Verify: builds clean

7. [ ] `Identity/SolhigsonPermission.cs` — replace `[Newtonsoft.Json.JsonIgnore]` with `[System.Text.Json.Serialization.JsonIgnore]` (if not already dual-attributed).
   - Verify: builds clean

8. [ ] `Data/PagedList.cs` — remove `[JsonProperty]` attributes. If STJ doesn't serialize read-only properties by default, add `[JsonInclude]`. Remove `using Newtonsoft.Json`.
   - Verify: builds clean

9. [ ] `Logging/Nlog/Targets/XUnitTestOutputHelperTarget.cs` — replace `JToken.Parse(output).ToString()` with `JsonNode.Parse(output)?.ToJsonString(new JsonSerializerOptions { WriteIndented = true })` (or `JsonDocument` for pretty-print). Remove `using Newtonsoft.Json.Linq`.
   - Verify: builds clean

10. [ ] Update remaining files that call `HelperFunctions.ToJsonObject()` or `Serializer` methods — `ApiTraceMiddleware.cs`, `SolhigsonMvcControllerBase.cs`, `PermissionsMiddleware.cs`, `ExceptionHandlingMiddleware.cs`, `MemoryCacheProvider.cs`, `RedisCacheProvider.cs`. These should compile after the return type changes — fix any type mismatches.
    - Verify: full solution builds clean

### Phase 3: ApiRequestService Redesign

11. [ ] Create `Web/Api/ApiRequest.cs` — static factory class with `Get`, `Post`, `Put`, `Delete` methods returning `ApiRequestBuilder`. Builder chains: `WithHeader`, `WithBearerToken`, `WithNamedClient`, `WithTimeout`, `WithContentType`, `WithExpectContinue`, `WithLogTrace`, `WithServiceName`, `WithServiceDescription`, `WithHttpContent`. `Build()` returns `ApiRequestDetails`.
    - Verify: builds clean

12. [ ] Update `Web/Api/ApiRequestDetails.cs` — ensure compatibility with builder output. Change `ExpectContinue` default from `true` to `false`. Add body serialization: if builder received an `object? body`, serialize to JSON string in `Build()` using STJ. Store serialized payload in `Payload` property.
    - Verify: builds clean

13. [ ] Redesign `Web/Api/IApiRequestService.cs` — replace all 15+ methods with 2: `SendAsync(ApiRequestDetails, CancellationToken)` and `SendAsync<T>(ApiRequestDetails, CancellationToken)`. Remove `UseConfiguration(Action<ApiConfiguration>)`.
    - Verify: builds clean (Framework project only — consumers will break)

14. [ ] Rewrite `Web/Api/ApiRequestService.cs`:
    - Add `CancellationToken` parameter, pass to `client.SendAsync(request, ct)` and `ReadAsStringAsync(ct)`
    - Replace `ExtractObject<T>` Newtonsoft deserialization with `System.Text.Json.JsonSerializer.Deserialize<T>()` from the response string
    - Remove `Task.Run()` for `SaveApiTraceData` — call synchronously (NLog async targets handle the write)
    - Remove outer `SendRequestAsync` exception swallowing (lines 197-217)
    - Add `using` on `HttpRequestMessage`
    - Remove `_configuration` / `_apiConfiguration` mutable state — move `ApiConfiguration` into `ApiRequestDetails` or remove if trace logging config moves to builder
    - Remove `UseConfiguration` method
    - Update `SaveApiTraceData` to use `Dictionary<string, string>` instead of `JObject` for headers
    - Remove all `using Newtonsoft.*`
    - Verify: full framework solution builds clean, zero Newtonsoft references remaining

### Phase 4: Remove Obsolete Members

15. [ ] `Extensions/LoggerExtensions.cs` — delete 7 `[Obsolete]` methods: `ELogTrace`, `ELogDebug`, `ELogInfo`, `ELogWarn`, `ELogError` (2 overloads), `ELogFatal`.
    - Verify: builds clean

16. [ ] `Data/Repository/RepositoryBase.cs` — delete 2 `[Obsolete]` methods: `Get(Expression)`, `Get<Tk>(Expression)`.
    - Verify: builds clean

17. [ ] `Web/Attributes/PermissionAttribute.cs` — delete 5 `[Obsolete]` properties: `Description`, `IsMenuRoot`, `IsMenu`, `MenuIndex`, `Icon`.
    - Verify: builds clean

### Phase 5: Final Cleanup & Version Bump

18. [ ] `Solhigson.Framework.csproj` — bump `<PackageVersion>` to `10.1.0`.
    - Verify: builds clean

19. [ ] Verify zero Newtonsoft references across entire solution: `grep -r "Newtonsoft" src/ --include="*.cs"` returns empty (excluding Playground).
    - Verify: zero matches

20. [ ] Run all framework tests: `dotnet test`.
    - Verify: all tests pass

### Phase 6: Elfrique Consumer Update (after framework NuGet publish)

21. [ ] Update `src/Directory.Packages.props` — bump `Solhigson.Framework.Core` and `Solhigson.Utilities` to `10.1.0`.
    - Verify: `dotnet restore` succeeds

22. [ ] Refactor 4 providers (`PaystackPaymentProvider`, `FlutterwavePaymentProvider`, `TiqwaFlightProvider`, `KoraPayIdentityService`) — replace `IHttpClientFactory` injection with `IApiRequestService`. Use `ApiRequest.Post(url, body).WithBearerToken(key).WithNamedClient(Constants.XxxHttpClient).WithTimeout(seconds).Build()` pattern. Remove manual `HttpClient` creation, manual exception handling (framework handles it), manual logging (framework traces automatically).
    - Verify: builds clean

23. [ ] Update `Jobs/NonRecurring/GetIpInfo.cs` — replace `ServicesWrapper.ApiRequestService.GetDataJsonAsync(url).Result` with `SendAsync` call.
    - Verify: builds clean

24. [ ] Codify rule: `.claude/rules/outbound-http.md` — all outbound HTTP calls MUST use `IApiRequestService`, MUST NOT use raw `IHttpClientFactory`/`HttpClient` directly.
    - Verify: rule file exists, references skill for detail

25. [ ] Full Elfrique build and test: `dotnet build src/Elfrique.slnx`.
    - Verify: zero errors

## Verification

- [ ] Zero `using Newtonsoft` in any `.cs` file across both framework packages (excluding Playground)
- [ ] Zero `JObject`/`JToken`/`JProperty`/`JArray` references in framework/utilities source
- [ ] `Newtonsoft.Json` NuGet removed from both `.csproj` files
- [ ] `IApiRequestService` has exactly 2 public methods: `SendAsync` and `SendAsync<T>`
- [ ] All `SendAsync` methods accept `CancellationToken`
- [ ] No `Task.Run()` in `ApiRequestService`
- [ ] No `[Obsolete]` members remain in framework
- [ ] Both packages at version `10.1.0`
- [ ] `dotnet test` passes for framework
- [ ] `dotnet build` passes for Elfrique after consumer update
- [ ] No raw `IHttpClientFactory` usage in Elfrique providers

## Test Strategy

- **Framework unit tests**: `dotnet test` — existing tests validate serialization, repository, and logging behavior
- **Build verification**: zero errors on framework solution and Elfrique solution
- **Serialization compatibility**: verify `SerializeToJson`/`DeserializeFromJson` produce equivalent output for common types (camelCase, null handling, reference loops)
- **Manual**: Elfrique payment initiation + verification flow unchanged after provider refactor
- **Grep audit**: confirm zero Newtonsoft references, zero raw HttpClient usage in providers
