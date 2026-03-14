# AJAX Pattern

All AJAX calls MUST use `solhigson.getData` / `solhigson.postData` utilities. MUST NOT use raw `$.ajax`, `$.post`, `$.get`, or `fetch`. AJAX endpoints MUST live in MVC controllers, MUST return `ResponseInfo` via `.HttpOk()`.

For client-side utilities, CSRF setup, success check modes, and server-side patterns, MUST invoke the `ajax` skill.
