## Migration Safety

### Two-Phase Approach for Breaking Changes

MUST NEVER deploy a breaking schema change (column drop, type change, rename) in the same release that changes the code. MUST use two deployments:

**Phase 1 — Code handles both schemas:**
```
Deploy: Code that reads from new column but falls back to old column
Migration: Add new column, copy data, keep old column
```

**Phase 2 — Remove old schema:**
```
Deploy: Code only uses new column (old column references removed)
Migration: Drop old column
```

### Column Rename Example
1. Add new column → `ALTER TABLE Orders ADD NewColumnName NVARCHAR(200)`
2. Migrate data → `UPDATE Orders SET NewColumnName = OldColumnName`
3. Deploy code using new column (with fallback reading old if new is null)
4. Deploy code only using new column
5. Drop old column → `ALTER TABLE Orders DROP COLUMN OldColumnName`

### Safe Migration Rules
- MUST NEVER drop columns in the same deployment that stops using them
- MUST NEVER change column types without a two-phase approach
- MUST test migrations against a copy of production schema before deploying
- Backward-incompatible index changes (drop + recreate) MUST use `CREATE INDEX ... WITH (DROP_EXISTING = ON)` where possible
- MUST ALWAYS include rollback strategy in migration comments for non-trivial changes