## Why

Sonarr/Radarr send `limit` and `offset` on every search request, but FunkArr ignores both — `limit` is bound in `IndexerRequest` but never forwarded, and `offset` only appears in the RSS response header without slicing items. The MediathekViewWeb query hardcodes `Size: 50, Offset: 0`. This means clients always get the full result set regardless of what they asked for.

Separately, the ArrApi endpoint wiring manually resolves `IActorRef` and passes config values through method parameters (`MapIndexerApi(apiKey, searchGateway)`), even though Minimal API supports DI injection directly in handlers. This adds unnecessary coupling in `ApplicationSetupContainer`.

## What Changes

- Forward `limit` and `offset` from IndexerRequest through search commands to the MediathekViewWeb query
- Apply pagination when building the RSS response (slice items, set correct total)
- Update Caps limits to reflect realistic defaults (100 default, 500 max)
- Refactor `MapIndexerApi` and `MapDownloadApi` to resolve `IActorRegistry` and `IOptions<FunkArrOptions>` via DI instead of receiving them as parameters
- Simplify `ApplicationSetupContainer` to parameterless `app.MapIndexerApi()` / `app.MapDownloadApi()`

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `search-messages`: Add `Limit` (int?) and `Offset` (int?) to TvSearchCommand, MovieSearchCommand, and GeneralSearchCommand
- `search-gateway`: SearchManager passes limit/offset through to workers
- `tv-search`: TvSearchWorker uses limit/offset for MediathekQuery Size/Offset
- `movie-search`: MovieSearchWorker uses limit/offset for MediathekQuery Size/Offset
- `newznab-indexer-api`: Update Caps limits, apply pagination in ToRss, update endpoint signatures
- `application-bootstrap`: ApplicationSetupContainer calls parameterless Map methods

## Impact

- **FunkArr.Messages**: TvSearchCommand, MovieSearchCommand, GeneralSearchCommand gain Limit/Offset parameters
- **FunkArr.Search**: SearchManager, TvSearchWorker, MovieSearchWorker use pagination values
- **FunkArr.ArrApi**: IndexerApiEndpoints and DownloadApiEndpoints resolve deps from DI; no more parameter passing
- **FunkArr (Host)**: ApplicationSetupContainer simplified
- **Tests**: SearchManagerTests, TvSearchWorkerTests, MovieSearchWorkerTests need updated command constructors
