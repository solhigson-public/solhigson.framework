# Notifications — Reference

## Email Template Pattern

HTML fragment structure for email body templates.

### Compatibility Rules

- MUST inline all styles on every element — external/`<style>` blocks MUST NOT be the only style source
- MUST add `bgcolor` attribute on every colored `<td>` (Outlook ignores CSS background-color)
- MUST NOT use `rgba()` — solid hex colors only
- MUST NOT use `border-radius` for critical rendering (Outlook ignores it)
- MUST NOT use CSS gradients — solid colors only
- MUST use MSO conditional comments `<!--[if (gte mso 9)|(IE)]>` for Outlook table wrappers
- MUST add `role="presentation"` on all layout tables
- MUST use font stack: `'Segoe UI', Helvetica, Arial, sans-serif` — NEVER use web fonts in email
- MUST use `@media` with attribute selectors `td[class="..."]` for responsive breakpoints
- MUST use paragraph spacing: `margin: 0 0 14px 0`

### Fragment Structure

- MUST use `<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">` as root
- Each content block MUST be a `<tr><td>` row with full inline styles
- MUST add `bgcolor` attribute on every `<td>` with a background-color style
- Font stack MUST be `'Segoe UI', Helvetica, Arial, sans-serif` on every styled `<td>`

### Row Types

- **Heading**: 20px, font-weight 700, color `#111827`, padding `0 0 16px 0`
- **Body text**: 15px, color `#374151`, line-height 1.7, padding `0 0 14px 0`
- **Muted text**: 13px, color `#6B7280`, line-height 1.7
- **CTA button**: MUST use MSO conditional `<v:roundrect>` for Outlook + `<a>` fallback. Background `#2E7D32`, white text, 15px bold, padding `14px 32px`
- **Data box**: `<table>` inside `<td>` with `background-color: #f3f4f6; padding: 16px 20px; width: 100%`, 14px text, line-height 1.8, `<strong>` labels
- **Divider**: `border-top: 1px solid #e5e7eb` on `<td>` with `padding-top: 20px`
- **Closing**: "Best regards," + `<br/>` + linked `[[applicationName]]` Team, color `#2E7D32`, font-weight 600

### Placeholders

- MUST use `[[placeholder]]` syntax matching constants in `EmailPlaceholders.cs`
- `[[Name]]`, `[[applicationName]]`, `[[platformWebsite]]`, `[[supportEmail]]` MUST appear in every transactional email template

---

## Notification Dispatch

How to dispatch notifications from service methods using `NotifyAsync` on `ServiceBase`.

### Dispatch

- MUST call `NotifyAsync` after `SaveChangesAsync()` — entity MUST be persisted before notification
- MUST wrap notification calls in try-catch — notification failure MUST NOT fail the business operation
- MUST look up recipient user info (`RecipientEmail`, `RecipientName`, `RecipientUserId`) from `DbContext.Users`

### UserNotificationRequest

- All channel flags (`SendEmail`, `CreateInAppNotification`, `SendPushNotification`, `SendSms`) default to `false` — MUST explicitly set `true` for each desired channel
- `EmailTemplate` MUST match the HTML filename (sans `.html`) in `EmailTemplates/` — seeder auto-discovers and uses filename as DB key
- `EmailSubject` MUST be set when `SendEmail = true` — `NotifyAsync` prepends `Constants.ApplicationName + " - "`
- `TemplatePlaceholders` MUST use `EmailPlaceholders.*` constants as keys — MUST NOT use raw strings
- `NotifyAsync` auto-injects `EmailPlaceholders.Name` from `RecipientName` — callers MUST NOT add it manually

### In-App Notifications

- `NotificationType` MUST use values from `NotificationType` enum (Domain layer)
- `NotificationTitle` MUST be a short human-readable title (e.g., "Order Confirmed")
- `NotificationMessage` MUST include entity-specific context (order ref, tour name, etc.)
- `NotificationActionUrl` MUST link to the relevant detail page
