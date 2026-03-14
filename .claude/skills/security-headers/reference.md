# Security Headers (.NET) — Detailed Reference

ASP.NET Core implementation patterns for security headers.

## Middleware Registration Order
- Security header middleware MUST be registered BEFORE `UseStaticFiles()` and `UseRouting()`/MVC
- This ensures headers are set on all responses, including static file responses

## CSP Nonce Generation
- Generate a cryptographically random nonce per request:
  ```
  var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
  ```
- Set the CSP header with the nonce:
  ```
  context.Response.Headers["Content-Security-Policy"] =
      $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'nonce-{nonce}'";
  ```
- Store the nonce in HttpContext for Razor access:
  ```
  context.Items["CspNonce"] = nonce;
  ```

## Razor Usage
- Render nonce on inline script tags:
  ```
  <script nonce="@Context.Items["CspNonce"]">...</script>
  ```
- Consider a tag helper to inject the nonce automatically on `<script>` and `<style>` tags

## Other Headers
- Set remaining headers in the same middleware:
  ```
  context.Response.Headers["X-Content-Type-Options"] = "nosniff";
  context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
  context.Response.Headers["X-Frame-Options"] = "DENY";
  ```
- MUST remove `Server` header via Kestrel options: `options.AddServerHeader = false`

## NuGet Packages
- **NetEscapades.AspNetCore.SecurityHeaders** — optional helper that provides a fluent API for header configuration and nonce management
