# Pagination (.NET)

MUST implement keyset pagination using composite clustered PK `(Created, Id)` with `Where(x => x.Created > cursor || (x.Created == cursor && x.Id > cursorId))`. MUST return cursor token in response for next-page requests. MUST NOT use `.Skip()` / `.Take()` with page numbers.

For EF Core query patterns, MUST invoke the `efcore` skill.
