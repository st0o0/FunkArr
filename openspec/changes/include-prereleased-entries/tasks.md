# Tasks

## 1. Queries

- [x] 1.1 Make `TvSearchWorkerState` / `MovieSearchWorkerState` include pre-released entries in every search. Verify with state tests for TV and movie queries.
- [x] 1.2 Query with `future=true` in `MediathekApiEndpoints` for `/api/mediathek/search`. Verify with an integration test asserting `future=true` on the query sent to the Mediathek manager probe.
- [x] 1.3 Remove the interim `FunkArr:Search:IncludeFuture` option (`SearchOptions`) and all consumers. Verify no reference remains and the build passes.

## 2. Docs and verification

- [x] 2.1 Describe the behaviour and the Sonarr air date restriction in how-it-works (both languages) and remove the search section from the configuration docs.
- [x] 2.2 Verify manually from Germany that pre-released entries are downloadable.
- [x] 2.3 Run `dotnet format --verify-no-changes` and the affected test projects via CI and verify they pass.
