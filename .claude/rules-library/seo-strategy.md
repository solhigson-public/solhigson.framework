---
name: SEO Strategy
description: "SEO implementation — title format, robots directives per page type, JSON-LD structured data, semantic HTML, heading hierarchy"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing view templates
  - writing UI markup
depends_on:
  - design-language
---

# SEO Strategy

MUST use title format: `Page Title | {AppName}`. MUST set robots directives: Public pages `index, follow`; auth/admin pages `noindex, nofollow`. MUST include JSON-LD: Organization + WebSite on all pages.

For semantic HTML, heading hierarchy, lazy loading, link attributes, and full SEO checklist, MUST invoke the `design-language` skill.
