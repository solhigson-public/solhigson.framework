# Log Redaction (.NET)

MUST annotate PII properties on DTOs/entities with `[PersonalData]` attribute. MUST configure `Microsoft.Extensions.Compliance.Redaction` in DI for automatic log redaction. MUST use structured logging with named parameters — redaction operates on parameter values, not interpolated strings.

For implementation patterns, MUST invoke the `observability` skill.
