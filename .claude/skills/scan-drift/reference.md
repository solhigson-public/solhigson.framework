## Drift Check: Layer Violations

### Web → Infrastructure (Critical)
Grep for Infrastructure namespace imports in Web projects:
```
Pattern: using {AppName}.Infrastructure
Search in: **/Web.Ui/**/*.cs, **/Web.Hangfire/**/*.cs
Exclude: Program.cs, Startup.cs (DI registration is allowed)
```
Web layer MUST only reference Application and Domain namespaces for business logic.

### Domain → Application/Infrastructure (Critical)
```
Pattern: using {AppName}.Application OR using {AppName}.Infrastructure
Search in: **/Domain/**/*.cs
Expected: zero matches
```
Domain has zero dependencies on other layers.

### Application → Infrastructure (Critical)
```
Pattern: using {AppName}.Infrastructure
Search in: **/Application/**/*.cs
Expected: zero matches
```
Application depends only on Domain.

---

## Drift Check: Auth Attribute Coverage

### Find all controller actions
```
Pattern: \[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)
Search in: **/Controllers/**/*.cs
```

### For each action, check the method and its class for auth attributes
```
Required (one of): [AllowAnonymous], [Permission(...)], [Authorize]
```

### Analysis logic
1. Find all methods decorated with `[Http*]` attributes
2. For each method, check method-level attributes AND class-level attributes
3. If neither method nor class has an auth attribute → **Critical** finding
4. Report: `{Controller}.{Action} — missing auth attribute`

---

## Drift Check: Service Return Types

### Find public service methods
```
Pattern: public.*async.*Task<(?!ResponseInfo)
Search in: **/*Service.cs (exclude *FacadeService.cs for separate check)
Exclude: ServiceBase methods, override methods
```

### Expected
All public service methods return `Task<ResponseInfo>` or `Task<ResponseInfo<T>>`.

### Exceptions
- Private/protected helper methods may return other types
- Facade services return composite data records (not ResponseInfo) — check separately

---

## Drift Check: Entity Conventions

### Find entity classes
```
Pattern: class \w+ : EntityBase
Search in: **/Domain/Entities/**/*.cs
```

### Verify record declaration
```
Expected: record \w+ : EntityBase (not class)
```

### Check ID pattern
Entities MUST use string IDs via `MassTransit.NewId.NextSequentialGuid()`.

---

## Drift Check: Facade Violations

### Find multi-service controller actions
```
Pattern: ServicesWrapper\.\w+Service\.\w+
Search in: **/Controllers/**/*.cs
```

### Analysis logic
1. For each controller action method, count distinct `ServicesWrapper.*Service.*` calls
2. If count >= 2 → **Warning**: candidate for facade extraction
3. Also check: if action calls a facade AND another service → **Warning**: move the extra call into the facade

---

## Drift Check: Generated File Edits

### Check git history
```bash
git log --all --diff-filter=M -- '*.generated.cs'
```

### Expected
Zero commits modifying `.generated.cs` files. Any match is a **Warning** — these files are auto-generated and MUST NEVER be hand-edited.

---

## Report Format

```
=== Architectural Drift Scan ===
Project: {project-name}
Date: {date}

CRITICAL ({count})
  [LAYER] Web.Ui/Controllers/OrdersController.cs:3 — imports Infrastructure namespace
  [AUTH]  AdminController.DeleteUser — missing auth attribute

WARNING ({count})
  [RETURN] OrderService.GetTotalAsync:45 — returns Task<decimal> instead of ResponseInfo<decimal>
  [FACADE] HomeController.Index:12 — calls 3 services directly (candidate for facade)

INFO ({count})
  [ENTITY] AuditLog:1 — declared as class, not record

Summary: {critical} critical, {warning} warnings, {info} info
```
