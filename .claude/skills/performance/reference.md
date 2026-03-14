# Performance, Defensive Programming & Secure Coding — Reference

## Performance

- MUST design all code to handle high throughput (2000+ TPS target)
- MUST avoid heap allocations in hot paths — MUST use stack allocation and buffer reuse over heap allocation
- MUST use asynchronous I/O consistently — MUST NEVER block on async operations
- MUST cache aggressively — MUST avoid repeated lookups, use static/singleton caches for process-lifetime data
- MUST prefer efficient string building over concatenation in hot paths
- MUST use async-compatible synchronization primitives over blocking locks
- MUST avoid unnecessary object creation per request — reuse, pool, or eliminate

## Defensive Programming

- MUST validate all inputs at system boundaries — null checks, range checks, format validation
- MUST fail fast with clear error messages — MUST NEVER silently swallow exceptions without logging
- MUST use guard clauses over nested conditionals
- MUST assume external data is untrusted — deserialize defensively, handle malformed payloads
- MUST prefer safe parsing methods (try-parse pattern) over parse-and-catch for expected failure paths
- External call timeouts: MUST comply with `resilience.md`
- MUST log actionable context on errors — include correlation IDs, operation names, relevant parameters

## Secure Coding

- MUST NEVER log secrets, tokens, API keys, or PII — MUST sanitize before logging
- MUST use parameterized queries exclusively — MUST NEVER string-concatenate SQL
- MUST validate and sanitize all user input — protect against injection (SQL, XSS, command)
- MUST use constant-time comparison for security-sensitive string comparisons (tokens, hashes)
- MUST store secrets in configuration/vault — MUST NEVER hardcode in source
- MUST NEVER provide default/fallback values for API keys, secrets, or credentials in configuration accessors — missing config MUST throw immediately
- MUST apply principle of least privilege — request only necessary permissions/scopes
- MUST encrypt sensitive data in transit and at rest
- MUST set timeouts and rate limits on all external-facing endpoints
