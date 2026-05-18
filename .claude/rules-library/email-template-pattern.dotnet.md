---
name: Email Template Pattern (.NET)
description: "HTML email body templates — table-based layout, inline styles, bgcolor on colored TDs, MSO conditionals for Outlook, [[placeholder]] syntax"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing view templates
  - writing UI markup
depends_on:
  - email-template-pattern
---

# Email Template Pattern

HTML fragment structure for email body templates. MUST use table-based layout with inline styles, `bgcolor` on every colored `<td>`, MSO conditionals for Outlook, `[[placeholder]]` syntax.

For fragment structure, row types, CTA buttons, data boxes, and placeholder conventions, MUST invoke the `notifications` skill.
