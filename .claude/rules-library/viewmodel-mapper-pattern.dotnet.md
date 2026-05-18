---
name: ViewModel Mapper Pattern (.NET)
description: "DTO-to-ViewModel mapping — static mapper classes in Web.Ui/Helpers, one mapper per controller area, shared logic extraction, 3+ action threshold for adoption"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
---

# ViewModel Mapper Pattern

When controllers convert DTOs to ViewModels, MUST use a static mapper class. For full examples, MUST invoke the `dotnet-app` skill.

- MUST use **static class with static methods** — no state, no DI. MUST place in `Web.Ui/Helpers/`.
- MUST create **one mapper per controller area** — `PublicViewModelMapper`, `AdminViewModelMapper`, etc.
- **Controller MUST stay thin** — service -> mapper -> `View(viewModel)`. MUST NOT do inline property-by-property mapping.
- MUST extract **shared logic** (logic reused in 2+ mapping methods, or the same mapping used in 3+ actions) into private helper methods within the mapper (e.g., `FormatPrice()`, `CalculateInitials()`).
- MUST use when 3+ actions map DTOs to ViewModels, or same mapping is reused in multiple places.
- MUST NOT use for simple 1:1 Mapster projections (`dto.Adapt<ViewModel>()`), or API controllers returning DTOs.
