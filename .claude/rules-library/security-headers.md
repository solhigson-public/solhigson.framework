---
name: Security Headers
description: "HTTP security headers — CSP, X-Content-Type-Options, Referrer-Policy, X-Frame-Options; inline script audit against CSP; unsafe-inline exception process"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing auth or security code
  - writing deployment config
---

# Security Headers

Language-agnostic principles for HTTP security headers.

## Required Headers
- **Content-Security-Policy**: controls which resources the browser is allowed to load — primary defense against XSS
- **X-Content-Type-Options**: MUST set to `nosniff` — prevents MIME type sniffing attacks
- **Referrer-Policy**: MUST set to `strict-origin-when-cross-origin` — limits referrer leakage to third parties
- **X-Frame-Options**: MUST set to `DENY` or `SAMEORIGIN` — prevents clickjacking via iframe embedding

## Deprecated Headers
- MUST remove `X-Xss-Protection` header — browser XSS auditors are deprecated and can introduce vulnerabilities

## Content-Security-Policy Principles
- MUST set `default-src 'self'` as baseline — only allow same-origin by default
- MUST use nonce-based `script-src` (`'nonce-{value}'`) — MUST NOT use `'unsafe-inline'`
- MUST use nonce or hash for `style-src` — MUST NOT use `'unsafe-inline'`
- MUST NOT use `'unsafe-eval'` — blocks `eval()`, `Function()`, and similar dynamic code execution
- MUST whitelist external domains explicitly when required (CDNs, analytics)

## Inline Script Audit Checklist
Before adding any inline `<script>` block, MUST verify all of:
1. CSP nonce is applied to the script tag
2. Business reason is documented (comment or ADR)
3. No alternative exists (external script file, event handler attribute, data attribute)

## Fail-Closed Policy
- MUST fail-closed: if CSP blocks a script, fix the CSP violation — MUST NOT weaken the policy
- MUST test CSP in `Content-Security-Policy-Report-Only` mode before enforcing
- MUST set up a report-uri or report-to endpoint to collect violation reports during rollout
