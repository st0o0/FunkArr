# Tasks

## 1. Feed

- [x] 1.1 Query the feed ingest with `future=true`. Verify with a feed crawler test asserting the flag.
- [x] 1.2 Drop entries more than 90 days ahead (`FeedIngestPlanner.WithinLead`). Verify with a planner test.

## 2. Search

- [x] 2.1 Keep `future=false` for TV/movie searches and the internal Mediathek search; remove the interim option, two-query merge and adaptive paging. Verify with a search worker test asserting the flag and no remaining references.

## 3. Docs and verification

- [x] 3.1 Describe the search and feed behaviour and the Sonarr air date restriction in how-it-works (both languages); remove the search section from the configuration docs.
- [x] 3.2 Verify manually from Germany that pre-released entries are downloadable, measure future entries per show via the live API, and check Sonarr's search filters in its source.
- [ ] 3.3 Run `dotnet format --verify-no-changes` and the affected test projects via CI and verify they pass.
