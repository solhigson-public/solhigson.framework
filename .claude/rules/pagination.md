# Pagination

MUST use cursor-based (keyset) pagination for all list endpoints. MUST NOT use offset/page-number pagination — constant-time performance regardless of depth. MUST use "load more" / infinite scroll UX pattern.

For implementation details, MUST invoke the `efcore` skill (dotnet) or equivalent data access skill.
