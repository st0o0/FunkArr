## Why

`MediaIdentity` is a single flat record shared by both Show and Movie search flows, with all fields nullable. This allows impossible states: a movie with Season/Episode, a show with TmdbId/Year. `MovieSearchWorkerState` actively populates Season/Episode from scoring metadata even though they are meaningless for movies. Splitting into typed subtypes enforces correct invariants at compile time.

## What Changes

- Replace sealed `MediaIdentity` record with abstract base + two sealed subtypes: `ShowIdentity` (TvdbId, Season, Episode) and `MovieIdentity` (TmdbId, Year), sharing `ImdbId` on the base
- `TvSearchWorkerState.BaseIdentity` returns `ShowIdentity`, `MovieSearchWorkerState.BaseIdentity` returns `MovieIdentity`
- `MovieSearchWorkerState` stops passing Season/Episode from scoring metadata into identity
- `SceneRelease.ForShow`/`ForMovie` and `Expand()` pattern-match on the identity subtype to build flat wire types (`ExternalIds`, `MatchMetadata`)
- Wire types (`ExternalIds`, `MatchMetadata`) and scoring trace types (`TracedIdentification`, `MetadataSpec`, `EnrichmentTrace`) stay flat -- they serve protocol/diagnostic purposes where all-nullable is correct by design

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `search-pipeline-types`: `MediaIdentity` becomes an abstract base with `ShowIdentity`/`MovieIdentity` subtypes
- `search-worker-state`: Worker states return typed identity subtypes; `MovieSearchWorkerState` drops Season/Episode from identity construction

## Impact

- `FunkArr.Search`: `MediaIdentity.cs`, `TvSearchWorkerState.cs`, `MovieSearchWorkerState.cs`, `SceneRelease.cs`, `EnrichedItem.cs` (type of Identity property)
- `FunkArr.Search.Tests`: `SceneReleaseTests.cs`, worker state tests -- update `MediaIdentity` construction to use subtypes
- No changes to `FunkArr.Messages` (wire types stay flat)
- No changes to `FunkArr.Persistence` (no persistence records affected)
- No API contract changes (Newznab XML output unchanged)
