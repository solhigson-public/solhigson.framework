# Security Headers

MUST configure CSP, X-Content-Type-Options, Referrer-Policy, X-Frame-Options on all responses. MUST audit inline scripts against CSP policy before adding. MUST NOT allow `unsafe-inline` in production without documented exception. MUST remove deprecated X-Xss-Protection header.

For implementation patterns, MUST invoke the `security-headers` skill.
