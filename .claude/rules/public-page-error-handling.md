# Public Page Error Handling

Error handling strategy for public-facing MVC controller actions. For full code examples, MUST invoke the `dotnet-app` skill.

- **Browse/list pages**: on service error, MUST call `SetErrorMessage()` and MUST render with empty results. View's empty-state handles display.
- **Detail pages**: two distinct failures — entity not found -> MUST return `NotFound()` (404); service error -> MUST call `SetErrorMessage()` and MUST redirect to browse. MUST NEVER return 404 for transient errors.
- **Aggregation pages** (home/dashboard): on partial failure, MUST render what succeeded. MUST NOT show flash messages — empty sections acceptable on discovery surfaces.
- **Checkout/transactional**: no active order -> MUST redirect with `SetInfoMessage`. Order fetch fails -> MUST redirect with `SetErrorMessage`. MUST NEVER show a broken checkout page.
