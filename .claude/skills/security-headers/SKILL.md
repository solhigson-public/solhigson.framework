---
name: security-headers
description: "Security headers (.NET) — middleware setup, CSP nonce generation, header registration in ASP.NET Core pipeline"
user_invocable: true
---

# Security Headers (.NET Implementation)

ASP.NET Core security header configuration. For principles and header purposes, see the common `security-headers` skill.

## When This Skill Is Invoked
- When adding security header middleware to an ASP.NET Core application
- When implementing CSP nonce generation and propagation
- When rendering nonce attributes in Razor views

## Stack
- **Middleware**: custom inline middleware or NetEscapades.AspNetCore.SecurityHeaders
- **Nonce**: per-request generation via `RandomNumberGenerator`, passed through `HttpContext.Items`
- **Razor**: nonce rendered from `@Context.Items["CspNonce"]`

## Key Conventions
- Security header middleware MUST be registered BEFORE static files and MVC
- CSP nonce MUST be generated per request — MUST NEVER be static or cached
- All script and style tags requiring inline content MUST include the nonce attribute

MUST read `reference.md` for implementation patterns and middleware configuration.
