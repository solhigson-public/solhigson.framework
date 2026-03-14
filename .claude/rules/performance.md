# Performance, Defensive Programming & Secure Coding

2000+ TPS target. MUST use async I/O consistently. MUST validate all inputs at system boundaries. For log redaction (no PII, no secrets), MUST comply with `log-redaction.md`.

MUST enable response compression (Brotli preferred, Gzip fallback). Hot-path queries MUST use pre-compiled queries when profiling identifies translation cost as bottleneck. Core Web Vitals budgets: LCP < 2.5s, INP < 200ms, CLS < 0.1.

For detailed performance targets, memory allocation rules, defensive programming, and secure coding patterns, MUST invoke the `performance` skill.
