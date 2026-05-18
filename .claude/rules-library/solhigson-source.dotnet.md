---
name: Solhigson Framework Source
description: "Solhigson framework local source location — read types/APIs from local repo, never decompile NuGet DLLs, ResponseInfo/ResponseInfo<T> struct composition"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
---

# Solhigson Framework Source

Local source code for the Solhigson framework packages is at:
`C:\Users\eawag\My Drive\Source\Solhigson\solhigson.framework.core`

When you need to check Solhigson types, APIs, or class hierarchies, MUST read from this local source — MUST NOT decompile NuGet DLLs. If local source is unavailable or out of sync, MUST contact the framework maintainer before referencing the NuGet package directly.

## Key Types

- **`ResponseInfo`** and **`ResponseInfo<T>`** (`Solhigson.Utilities.Dto`) are **structs** — `ResponseInfo<T>` composes `ResponseInfo` via an internal field, it does not inherit from it.
