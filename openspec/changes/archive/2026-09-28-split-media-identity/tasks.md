## 1. Type Hierarchy

- [x] 1.1 Replace sealed `MediaIdentity` record with abstract base + `ShowIdentity` + `MovieIdentity` in `MediaIdentity.cs`

## 2. Worker States

- [x] 2.1 Update `TvSearchWorkerState.BaseIdentity` to return `ShowIdentity(ImdbId, TvdbId, null, null)` and fix all `with` expressions to use ShowIdentity
- [x] 2.2 Update `MovieSearchWorkerState.BaseIdentity` to return `MovieIdentity(ImdbId, TmdbId, null)` and remove Season/Episode from identity construction
- [x] 2.3 Update `MovieSearchWorkerState` fallback path (`BuildFallbackItems`) to use `MovieIdentity`

## 3. SceneRelease

- [x] 3.1 Update `SceneRelease.ForShow` to pattern-match on `ShowIdentity` for Season/Episode access
- [x] 3.2 Update `SceneRelease.ForMovie` to pattern-match on `MovieIdentity` for Year access
- [x] 3.3 Update `SceneRelease.Expand()` to build `ExternalIds` + `MatchMetadata` via pattern match on identity subtype

## 4. Tests

- [x] 4.1 Update `SceneReleaseTests` to use `ShowIdentity`/`MovieIdentity` instead of `MediaIdentity`
- [x] 4.2 Update `TvSearchWorkerStateTests` to assert `ShowIdentity` return type from `BaseIdentity`
- [x] 4.3 Update `MovieSearchWorkerState` tests (if any) to assert `MovieIdentity` and absence of Season/Episode
- [x] 4.4 Update `ReleaseDisplayTests` to use typed identities (no changes needed -- no MediaIdentity references)
- [x] 4.5 Run all test projects, fix any remaining compilation errors from the type change

## 5. Specs

- [x] 5.1 Sync delta specs to `openspec/specs/search-pipeline-types/spec.md` and `openspec/specs/search-worker-state/spec.md`
