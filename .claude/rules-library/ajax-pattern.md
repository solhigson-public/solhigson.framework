---
name: AJAX Pattern
description: "AJAX request patterns — fetch wrappers, CSRF setup, response validation, prohibition on raw browser APIs"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - writing client-side scripts
---

# AJAX Pattern

All AJAX calls MUST use project-standard utilities. MUST NOT use raw browser APIs directly. AJAX endpoints MUST return standardized response objects.

For client-side utilities and server-side patterns, MUST invoke the `ajax` skill.
