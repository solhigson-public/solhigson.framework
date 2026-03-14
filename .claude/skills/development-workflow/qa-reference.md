# QA Test Script Conventions — Reference

## ID Format

- `TC-{AREA}-{NNN}` — same area codes as user stories for traceability
- MUST NEVER reuse IDs — retired test cases keep their ID with a `Deprecated` note

## Structure

- Test case priority MUST be independent of user story priority — a P0 story can have P3 test cases (cosmetic checks) and a P2 story can have P0 test cases (data corruption risk)
- MUST have 15 steps max per test case — longer flows MUST be split into chained cases
- MUST use concrete test data values, MUST NOT use descriptions ("an invalid email") — use actual values (`not-an-email`)
- Negative cases MUST have their own test case ID — MUST NOT be buried inside happy-path cases
- One test script file per feature area, multiple test cases per file
- Every test case MUST reference a user story ID (`US-{AREA}-{NNN}`) or NFR ID (`NFR-{CAT}-{NNN}`)

## File Locations

- Test scripts: `.claude/qa/` — one file per feature area
- Template: `.claude/templates/qa-test-script-template.md`

## Test Execution

- **Smoke test**: P0 cases only
- **Regression**: P0 + P1 cases
- **Full suite**: All priorities — for release QA
- Test accounts: MUST use dedicated test accounts per role — MUST NOT share accounts across roles during test execution
