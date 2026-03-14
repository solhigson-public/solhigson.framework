# ViewModel Mapper Pattern

When controllers convert DTOs to ViewModels, MUST use a static mapper class. For full examples, MUST invoke the `dotnet-app` skill.

- MUST use **static class with static methods** — no state, no DI. MUST place in `Web.Ui/Helpers/`.
- MUST create **one mapper per controller area** — `PublicViewModelMapper`, `AdminViewModelMapper`, etc.
- **Controller MUST stay thin** — service -> mapper -> `View(viewModel)`. MUST NOT do inline property-by-property mapping.
- MUST extract **shared logic** (price formatting, initials) into private methods within the mapper.
- MUST use when 3+ actions map DTOs to ViewModels, or same mapping is reused in multiple places.
- MUST NOT use for simple 1:1 Mapster projections (`dto.Adapt<ViewModel>()`), or API controllers returning DTOs.
