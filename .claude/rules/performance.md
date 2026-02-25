# Performance, Defensive Programming & Secure Coding

## Performance
- Design all code to handle high throughput (2000+ TPS target)
- Minimize memory allocations in hot paths — prefer stack allocation and buffer reuse over heap allocation
- Use asynchronous I/O consistently — never block on async operations
- Cache aggressively — avoid repeated lookups, use static/singleton caches for process-lifetime data
- Prefer efficient string building over concatenation in hot paths
- Use async-compatible synchronization primitives over blocking locks
- Avoid unnecessary object creation per request — reuse, pool, or eliminate

## Defensive Programming
- Validate all inputs at system boundaries — null checks, range checks, format validation
- Fail fast with clear error messages — never silently swallow exceptions without logging
- Use guard clauses over nested conditionals
- Assume external data is untrusted — deserialize defensively, handle malformed payloads
- Prefer safe parsing methods (try-parse pattern) over parse-and-catch for expected failure paths
- Timeout all external calls — never allow unbounded waits
- Log actionable context on errors — include correlation IDs, operation names, relevant parameters

## Secure Coding
- Never log secrets, tokens, API keys, or PII — sanitize before logging
- Use parameterized queries exclusively — never string-concatenate SQL
- Validate and sanitize all user input — protect against injection (SQL, XSS, command)
- Use constant-time comparison for security-sensitive string comparisons (tokens, hashes)
- Store secrets in configuration/vault — never hardcode in source
- Apply principle of least privilege — request only necessary permissions/scopes
- Encrypt sensitive data in transit and at rest
- Set appropriate timeouts and rate limits on all external-facing endpoints
