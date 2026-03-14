# Security Headers (.NET)

MUST configure security headers in middleware (`UseSecurityHeaders()` or equivalent). MUST use CSP nonce generation for any required inline scripts. MUST register headers in the request pipeline before static files and MVC.

For CSP policy, nonce generation, and middleware patterns, MUST invoke the `security-headers` skill.
