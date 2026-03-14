# AJAX Pattern — Reference

All client-server AJAX interactions MUST use the `solhigson.web.js` utilities in `custom-script.js`. MUST NOT use raw `$.ajax`, `$.post`, `$.get`, or `fetch` for application AJAX calls.

## Client Side

### Utilities (available on all pages via `SharedScripts.cshtml`)
- **GET**: `solhigson.getData(url, showLoader, message, getRawData)` → jQuery Promise
- **POST**: `solhigson.postData(data, url, showLoader, message, postAsJson, expectJsonResult)` → jQuery Promise
- **Feedback**: `solhigson.DisplayInfo(message)` / `solhigson.DisplayError(message)`
- **Loader**: auto-managed via `$.blockUI` when `showLoader` is `true`

### CSRF
- MUST render antiforgery request token in a `<meta name="csrf-token">` tag in layouts
- MUST configure `$.ajaxSetup` globally to inject the token as a header on every request
- Header name MUST match the `HeaderName` configured in `AddAntiforgery()` in `Program.cs`

### Success Check
- **JSON mode** (default): utility auto-checks `data.statusCode === "90000"` — resolves on success, rejects with `data.message` on failure. Callers MUST NOT re-check `statusCode`.
- **Raw mode** (`getRawData: true`): resolves with the raw response (HTML partial or plain text), no `statusCode` check. MUST use this mode when the endpoint returns a rendered view.

## Server Side

- AJAX endpoints MUST live in MVC controllers (`MvcBaseController`) — same controllers that serve views
- JSON responses MUST return `ResponseInfo` / `ResponseInfo<T>` via `.HttpOk()` extension method
- HTML partial responses MUST return `PartialView()` (consumed via `getRawData: true` on client)
- Auth: `[AllowAnonymous]` for public, `[Authorize]` or `[Permission(...)]` for authenticated
