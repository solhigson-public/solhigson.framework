---
name: Solhigson JS Namespace
description: "Client-side JS conventions — solhigson.* utility namespace (logConsole, OpenAlert, Reload, Confirm), prohibited browser APIs, server-to-client variable pattern via hidden inputs"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing client-side scripts
  - writing UI markup
depends_on:
  - ajax-pattern.dotnet
---

# Solhigson JS Namespace

Project-authored client-side JavaScript MUST use `solhigson.*` utilities from `custom-script.js` when a solhigson equivalent exists for a browser API. The following browser APIs are prohibited — MUST use the listed replacement:

- MUST NOT use `console.*` methods (`console.log`, `console.error`, `console.warn`, `console.info`, `console.debug`) — MUST use `solhigson.logConsole()`
- MUST NOT use `alert()` — MUST use `solhigson.OpenAlert()`
- MUST NOT use raw `location.reload()` — MUST use `solhigson.Reload()`
- MUST NOT use `$.blockUI`/`$.unblockUI` directly — MUST use `solhigson.showLoader()`/`solhigson.hideLoader()`
- MUST NOT use `confirm()` or `window.confirm()` — MUST use `solhigson.Confirm(message, onConfirm, header)`. Native confirm blocks the browser thread and looks unpolished. `solhigson.Confirm` uses a styled Bootstrap modal (`#cs_confirmModal` in `Alert.cshtml`)
- For form submissions requiring confirmation, MUST use `data-confirm="message"` attribute on the `<form>` element. The global handler in `public.js` intercepts the submit event and shows `solhigson.Confirm` with an async callback pattern

For AJAX utilities (`getData`/`postData`), MUST comply with `ajax-pattern.dotnet.md`. For the full namespace method catalog, MUST invoke the `solhigson-js` skill.

## Server-to-Client Variable Pattern

Server-side values needed by `custom-script.js` MUST be rendered as hidden inputs in `Utils.cshtml` and read as variables in `custom-script.js`. MUST NOT hardcode server-derived values (cookie names, API keys, feature flags) directly in JS files.

Pattern: `Utils.cshtml` renders `<input type="hidden" value="@ServerValue" id="ejmVariableName"/>`, then `custom-script.js` reads `solhigson.VariableName = $("#ejmVariableName").val()`.
