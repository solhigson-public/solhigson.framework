---
name: Log Redaction
description: "PII protection in logs — no names/emails/IPs/phones in log output, pseudonymous identifiers only, redaction at logging boundary"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing monitoring or logging
  - writing code
depends_on:
  - observability
---

# Log Redaction

MUST NOT log PII (names, emails, IPs, phone numbers) in any log output. MUST use pseudonymous identifiers (user ID, correlation ID, session ID) for traceability. MUST redact PII at the logging boundary, not at individual call sites.

For implementation patterns, MUST invoke the `observability` skill.
