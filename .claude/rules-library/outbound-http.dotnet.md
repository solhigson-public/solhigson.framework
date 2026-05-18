---
name: Outbound HTTP (.NET)
description: "Outbound HTTP calls — IApiRequestService with fluent ApiRequest builder, prohibition of raw HttpClient/IHttpClientFactory/PostAsJsonAsync usage"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
---

# Outbound HTTP (.NET)

All outbound HTTP calls in services, facades, and controllers MUST use `IApiRequestService` with the fluent `ApiRequest` builder. MUST NOT use `IHttpClientFactory`, `HttpClient`, `PostAsJsonAsync`, `GetAsync`, or `ReadFromJsonAsync` directly — raw usage is thread-unsafe and bypasses framework trace logging. Exceptions: System layer (middleware, startup), test doubles (mocks), background jobs (if necessary for performance, document the exception).

For the `ApiRequest` builder pattern, `BuildRequest()` helper extraction rule, and response handling, MUST invoke the `dotnet-app` skill (see `references/services.md` — Outbound HTTP Pattern).
