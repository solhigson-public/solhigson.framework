---
name: View Helpers
description: "View helper requirements — mandatory helper methods for form controls, raw HTML prohibition, 2+ repetition extraction rule"
scope: common
tier: agent-injected
inject_policy: matched
work_types:
  - writing UI markup
  - writing view templates
---

# View Helpers

All form controls and interactive elements MUST use project-standard helper methods. Raw HTML form elements are PROHIBITED. Any UI pattern repeated 2+ times MUST be extracted into a reusable helper.

For helper catalog and extraction guidelines, MUST invoke the `razor-views` skill.
