---
name: Feature Flags
description: "Feature flag lifecycle — toggle without deployment, gradual rollout support, separation from app config, stale flag cleanup"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - designing architecture
  - writing deployment config
---

# Feature Flags

Language-agnostic principles for feature flag management.

## Flag Lifecycle
1. **Create** — define flag with name, description, owner, and expiry/review date
2. **Test** — verify both flag-on and flag-off paths in development/staging
3. **Gradual rollout** — enable for increasing segments (internal -> beta -> percentage -> GA)
4. **Full rollout** — flag is on for all users
5. **Remove flag** — delete flag definition from configuration
6. **Clean up code** — remove all conditional branching related to the flag

## Rollout Strategies
- **Percentage-based**: enable for N% of requests — useful for load testing and gradual exposure
- **User targeting**: enable for specific user IDs or groups — useful for beta programs
- **Time window**: enable between start and end dates — useful for seasonal features
- **Ring-based**: internal users -> beta cohort -> GA — useful for high-risk changes

## Flag Hygiene
- MUST set an expiry date or review date on every flag — flags without dates become permanent tech debt
- MUST clean up code after full rollout — conditional branches MUST be removed
- MUST audit flags quarterly — any flag past its expiry date MUST be resolved (extend with justification or remove)
- Stale flags increase code complexity and testing burden

## Naming
- MUST use descriptive PascalCase names: `NewCheckoutFlow`, `EnableBulkExport`, `V2PricingEngine`
- MUST NOT use generic names: `Flag1`, `TestFeature`, `Temp`, `NewStuff`

## Feature Flag vs Configuration
- Simple on/off settings (maintenance mode, max upload size) are configuration — NOT feature flags
- Feature flags MUST have rollout controls (percentage, targeting, time window)
- If it does not need gradual rollout, it is a configuration setting

## Kill Switch Pattern
- Critical features MUST have a kill switch flag that can disable the feature without deployment
- Kill switches MUST be evaluable without external dependencies (no remote flag service required)

## No Nesting
- MUST NOT nest feature flags — if Flag B only makes sense when Flag A is on, combine into a single flag or use a multi-state flag
- Nested flags create exponential testing combinations and confusing behavior

## Testing
- MUST test both flag-on and flag-off code paths
- MUST test flag transition behavior (what happens when flag changes mid-session)
