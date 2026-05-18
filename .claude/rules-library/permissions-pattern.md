---
name: Permissions and RBAC Pattern
description: "Authorization enforcement — endpoint-level auth declarations, generated permission constants, centralized view-layer permission checks"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing auth or security code
  - writing code
  - writing view templates
---

# Permissions & RBAC Pattern

Every endpoint MUST enforce authorization — MUST explicitly declare whether it requires authentication, specific permissions, or allows anonymous access. MUST NOT hardcode role or permission strings — MUST use generated constants or a centralized registry. Views MUST use centralized permission checks for conditional rendering — MUST NOT branch on role name strings directly.

For implementation patterns, MUST invoke the relevant stack skill.
