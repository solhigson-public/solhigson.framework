# Facade Service Pattern

When a controller action needs 2+ domain service calls, MUST use a facade service. 1:1 controller-to-facade — MUST NOT have cross-facade orchestration. Any call to any service counts, including cached/utility services.

For full facade patterns, naming, parallel execution, and error isolation, MUST invoke the `dotnet-app` skill.
