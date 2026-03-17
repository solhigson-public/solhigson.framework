# Outbound HTTP (.NET)

All outbound HTTP calls in the Application layer MUST use `IApiRequestService` with the fluent `ApiRequest` builder. MUST NOT use `IHttpClientFactory`, `HttpClient`, `PostAsJsonAsync`, `GetAsync`, or `ReadFromJsonAsync` directly — raw usage is thread-unsafe and bypasses framework trace logging.

For the `ApiRequest` builder pattern, `BuildRequest()` helper extraction rule, and response handling, MUST invoke the `dotnet-app` skill (see `references/services.md` — Outbound HTTP Pattern).
