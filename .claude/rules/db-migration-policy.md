# Database Migration Policy

MUST NOT make destructive schema changes in a single migration (drop column, rename column, change type). MUST use multi-step approach: add new → migrate data → remove old (across separate deployments). MUST validate migrations in staging before production. MUST support rollback — every migration MUST have a corresponding reverse migration.
