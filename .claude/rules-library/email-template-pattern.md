---
name: Email Template Pattern
description: "Email body construction — table-based layout, inline styles, placeholder syntax, MSO conditionals for Outlook compatibility"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing view templates
  - writing UI markup
---

# Email Template Pattern

Email body templates MUST use table-based layout with inline styles for cross-client compatibility. MUST use placeholder syntax for dynamic content. MUST include MSO conditionals for Outlook.

For fragment structure and placeholder conventions, MUST invoke the `notifications` skill.
