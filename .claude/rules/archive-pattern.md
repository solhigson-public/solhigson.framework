# Partition-Based Archive Pattern

For high-volume tables (millions of rows, time-series access, rarely accessed old data). Entity layer MUST use `I{Entity}` interface + `{Entity}Base` abstract record + active/archive concrete records. Archive entity MUST use `IEfCoreGenIgnore`.

For entity structure, migration, ArchiveService queries, Hangfire jobs, settings categories, and 3-tier lifecycle, MUST invoke the `efcore` skill.
