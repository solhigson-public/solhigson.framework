# Log Redaction

MUST NOT log PII (names, emails, IPs, phone numbers) in any log output. MUST use pseudonymous identifiers (user ID, correlation ID, session ID) for traceability. MUST redact PII at the logging boundary, not at individual call sites.

For implementation patterns, MUST invoke the `observability` skill.
