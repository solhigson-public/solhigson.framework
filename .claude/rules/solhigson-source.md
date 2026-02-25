# Solhigson Framework Source

Local source code for the Solhigson framework packages is at:
`C:\Users\eawag\My Drive\Source\Solhigson\solhigson.framework.core`

When you need to check Solhigson types, APIs, or class hierarchies, read from this local source — do not decompile NuGet DLLs.

## Key Types

- **`ResponseInfo`** and **`ResponseInfo<T>`** (`Solhigson.Utilities.Dto`) are **structs** — `ResponseInfo<T>` composes `ResponseInfo` via an internal field, it does not inherit from it.
