---
name: Permissions and RBAC Pattern (.NET)
description: "RBAC enforcement — every action requires AllowAnonymous/Permission/Authorize, generated Permission.* constants, Razor IsPermissionAllowedAsync checks, no role-name strings"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing auth or security code
  - writing code
  - writing view templates
depends_on:
  - permissions-pattern
---

# Permissions & RBAC Pattern

Every controller action MUST have exactly one of: `[AllowAnonymous]`, `[Permission(Permission.X)]`, or `[Authorize]`. Actions without any auth attribute are security bugs — MUST verify at code review time. MUST NEVER hardcode permission strings — MUST ALWAYS use generated `Permission.*` constants.

Razor views MUST use `this.IsPermissionAllowedAsync(Permission.X)` or NavBarHelper menu-driven checks (e.g., `menuList.Any()`) for conditional rendering based on user role. MUST NOT use `currentUser.Role?.Name`, `User.IsInRole()`, or any other role name string comparison in Razor views — role-based branching bypasses the centralized RBAC system and creates maintenance blind spots when roles are renamed or restructured.

For permission definition, controller usage, enforcement (build-time + runtime), antiforgery, and service call counting, MUST invoke the `dotnet-app` skill.
