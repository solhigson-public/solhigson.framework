---
name: Pagination (.NET)
description: "Keyset pagination — composite clustered PK (Created, Id), base64 cursor token, descending order queries, prohibition of Skip/Take page-number pagination"
scope: dotnet
tier: agent-injected
inject_policy: matched
work_types:
  - writing code
  - writing performance-sensitive code
depends_on:
  - pagination
---

# Pagination (.NET)

MUST implement keyset pagination using composite clustered PK `(Created, Id)`. Cursor token is base64-encoded pair `{created:iso8601},{id:string}`. Query MUST use descending order (newer first): `Where(x => x.Created < cursor || (x.Created == cursor && x.Id < cursorId))`. Entities with null Created MUST be sorted after (treat as latest). MUST return cursor token in response for next-page requests. MUST NOT use `.Skip()` / `.Take()` with page numbers.

For EF Core query patterns, MUST invoke the `efcore` skill.
