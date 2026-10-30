# Tasks

## 1. Option

- [x] 1.1 Add `SearchOptions` (`FunkArr:Search`, `IncludeFuture = true`) to `FunkArr.Core` and bind it with `ValidateOnStart` in `SearchSetupContainer`. Verify with a unit test for the default.

## 2. Queries

- [x] 2.1 Pass `includeFuture` into `TvSearchWorkerState.From` / `MovieSearchWorkerState.From` (default `true`) and use it in all four `QueryMediathek` constructions. Verify with state tests for both query paths of each state and for `false`.
- [x] 2.2 Inject `IOptionsMonitor<SearchOptions>?` (optional) into `TvSearchWorker` / `MovieSearchWorker` and pass `CurrentValue.IncludeFuture` to the state. Verify with `dotnet build` and the existing worker tests passing.
- [x] 2.3 Use the option in `MediathekApiEndpoints` for `/api/mediathek/search`. Verify with an integration test asserting `future=true` on the query sent to the Mediathek manager probe.

## 3. Docs and verification

- [x] 3.1 Document `FunkArr__Search__IncludeFuture` in `docs/en/configuration.md` and `docs/configuration.md`, including the Sonarr air date restriction as the way to wait for the broadcast. Verify both languages describe the same default.
- [x] 3.2 Run `dotnet format --verify-no-changes` and the affected test projects via CI and verify they pass.

## Workflow follow-up

- Open the PR stacked on `feat/variant-detection` (#207), referencing #205.
