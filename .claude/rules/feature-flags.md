# Feature Flags

Features with rollout control MUST use a feature flag system — MUST support toggling without deployment. MUST support gradual rollout (percentage, targeting, time windows). Simple on/off configuration and feature flags with rollout controls are separate concerns — MUST NOT conflate them. MUST clean up stale flags after full rollout.

For implementation patterns, MUST invoke the `feature-flags` skill.
