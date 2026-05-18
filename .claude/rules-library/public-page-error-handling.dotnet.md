---
name: Public Page Error Handling (.NET)
description: "Public-facing MVC error handling — browse pages render empty on error, detail pages distinguish 404 vs redirect, aggregation pages omit failed sections, checkout redirects on failure"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
---

# Public Page Error Handling

Error handling strategy for public-facing MVC controller actions. For full code examples, MUST invoke the `dotnet-app` skill.

- **Browse/list pages**: on service error, MUST call `SetErrorMessage()` and MUST render with empty results. View's empty-state handles display.
- **Detail pages**: two distinct failures — entity not found -> MUST return `NotFound()` (404); service error -> MUST call `SetErrorMessage()` and MUST redirect to browse. MUST NEVER return 404 for transient errors.
- **Aggregation pages** (dashboard, homepage): on partial failure (one or more widgets fail, others succeed), MUST render all successful sections as normal and omit failed sections (empty or hidden). MUST NOT show flash messages — treat timeouts and service exceptions identically. On complete failure (all widgets fail): MUST render the page with all sections empty and no error alerts.
- **Checkout/transactional**: no active order -> MUST redirect with `SetInfoMessage`. Order fetch fails -> MUST redirect with `SetErrorMessage`. MUST NEVER show a broken checkout page.
