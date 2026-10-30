# Tasks

## 1. Queries

- [x] 1.1 Remove `Future` from `QueryMediathek`; `MediathekQueryBuilder` always sends `future=true`. Verify with builder tests.
- [x] 1.2 Add `MediathekQueryPlan` (adaptive paging for `timestamp desc`, max 3 pages, de-duplication, 90-day lead cap) and drive it from `MediathekViewWebManager`. Verify with plan tests (single page, flooded page, end of results, max pages, lead cap, duplicates, other sort order).
- [x] 1.3 Let the search workers send a single `QueryMediathek`. Verify with search worker and state tests.
- [x] 1.4 Remove the interim `FunkArr:Search:IncludeFuture` option (`SearchOptions`) and the two-query merge. Verify no reference remains and the build passes.

## 2. Docs and verification

- [x] 2.1 Describe the behaviour and the Sonarr air date restriction in how-it-works (both languages) and remove the search section from the configuration docs.
- [x] 2.2 Verify manually from Germany that pre-released entries are downloadable, and measure future entries per show via the live API.
- [ ] 2.3 Run `dotnet format --verify-no-changes` and the affected test projects via CI and verify they pass.
