---
name: Modern C# (.NET)
description: "C# static analysis and code style — TreatWarningsAsErrors, EnforceCodeStyleInBuild, .editorconfig category-level severity, analyzer diagnostic suppression policy"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
depends_on:
  - code-analysis
---

# Modern C# (.NET)

Implements `code-analysis.md` for the .NET stack.

## Required Project Config

`Directory.Build.props` MUST include:
```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<AnalysisLevel>latest-recommended</AnalysisLevel>
```

`src/.editorconfig` MUST use **category-level defaults at `warning`**:
```ini
dotnet_analyzer_diagnostic.category-Style.severity = warning
dotnet_analyzer_diagnostic.category-CodeQuality.severity = warning
dotnet_analyzer_diagnostic.category-Design.severity = warning
dotnet_analyzer_diagnostic.category-Globalization.severity = warning
dotnet_analyzer_diagnostic.category-Naming.severity = warning
dotnet_analyzer_diagnostic.category-Performance.severity = warning
dotnet_analyzer_diagnostic.category-Reliability.severity = warning
dotnet_analyzer_diagnostic.category-Security.severity = warning
dotnet_analyzer_diagnostic.category-Usage.severity = warning
```

Individual suppressions MUST use `dotnet_diagnostic.{ID}.severity = suggestion` (or `none`) with a `#` justification comment.

## Verification

Before writing or editing C# code, MUST read `src/.editorconfig` if not already read in this session. Reading the file establishes which rules are active at which severity.

MUST check `dotnet build` output for diagnostics on changed files before committing. MUST comply with all active diagnostic rules (severity: warning or higher).
