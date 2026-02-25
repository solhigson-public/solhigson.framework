# Project-Specific File Naming

Project-specific rule, command, and skill files must include the project name as a suffix before the extension:

```
<name>.<projectname>.md
```

Examples:
- `gc-workflow.accountswitch.md` — project-specific to AccountSwitch
- `test-pattern.accountswitch.md` — project-specific to AccountSwitch

Files without a project name suffix are assumed to come from a stack or global scope. This makes it immediately clear which files are project-scoped vs inherited from stacks/global.
