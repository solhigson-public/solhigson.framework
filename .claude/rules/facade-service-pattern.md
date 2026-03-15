# Facade Service Pattern

When a controller action needs 2+ domain service calls, MUST use a facade service. 1:1 controller-to-facade — MUST NOT have cross-facade orchestration. Any call to any service counts, including cached/utility services.

Facades MUST inherit `FacadeServiceBase` (NOT `ServiceBase`). Facades are pure orchestrators — MUST NOT access repositories directly. Facades MUST live in `src/Elfrique.Application/Facades/` (NOT in `Services/`). MUST NOT create a facade for single-service-call actions — call the domain service directly.

For full facade patterns, naming, parallel execution, WhenAllAsync structured cancellation, and error isolation, MUST invoke the `dotnet-app` skill.
