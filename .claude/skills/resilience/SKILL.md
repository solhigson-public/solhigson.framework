---
name: resilience
description: Retry, circuit breaker, and timeout patterns for external service calls with externalized configuration.
---

# Resilience Patterns

Detailed conventions for building resilient external service integrations. Covers retry with exponential backoff, circuit breaker state management, explicit timeouts, and per-integration configuration.

## When This Skill Is Invoked
- When implementing or modifying external service calls (HTTP APIs, payment gateways, third-party integrations)
- When adding retry, circuit breaker, or timeout logic
- When reviewing resilience patterns in existing code

## Quick Reference
- **Retry**: exponential backoff with jitter, per-integration config, non-retryable errors short-circuit
- **Circuit breaker**: consecutive failure tracking, open/half-open/closed states, logged transitions
- **Timeouts**: explicit on every external call, externalized in configuration
- **Configuration**: all settings externalized per integration, NEVER hardcoded

MUST read `reference.md` for full directive details and implementation patterns.
