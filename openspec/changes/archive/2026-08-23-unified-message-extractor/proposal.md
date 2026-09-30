## Why

Five near-identical `HashCodeMessageExtractor` subclasses exist across the codebase, each doing the same thing: extracting a string entity key from a message. This duplication adds unnecessary files, forces every new shard region to create a boilerplate class, and scatters entity-key logic between messages and extractors. Additionally, the `Search/` (23 files) and `DownloadClient/` (22 files) folders have grown into flat dumps mixing unrelated concerns, making navigation harder as the project grows.

## What Changes

- Introduce a single `IShardedMessage` interface with an `EntityKey` property
- Make `IWithNzoId` extend `IShardedMessage` via default interface method (no breaking change to existing implementors)
- Fix `MovieSearchPipeline.Search` to derive its entity key from `ImdbId`/`Query` instead of accepting a raw `EntityKey` parameter — **BREAKING** (message shape changes)
- All sharded message types implement `IShardedMessage`
- Replace all 5 per-entity `MessageExtractor` classes with one `ShardedMessageExtractor`
- Restructure `Search/` into subfolders: `Pipelines/`, `Resolvers/`, `Matching/`, `Quality/` with corresponding namespace changes
- Restructure `DownloadClient/` into subfolders: `Pipeline/`, `Queue/`, `Tracker/`, `Ffmpeg/` with corresponding namespace changes

## Capabilities

### New Capabilities
- `sharded-message-contract`: Unified `IShardedMessage` interface and generic `ShardedMessageExtractor` replacing all per-entity extractors

### Modified Capabilities
- `tv-search-pipeline`: Search record implements `IShardedMessage`, namespace moves to `FunkArr.Search.Pipelines`
- `text-search-pipeline`: Search record implements `IShardedMessage`, namespace moves to `FunkArr.Search.Pipelines`
- `download-coordinator`: Messages use `IShardedMessage` via `IWithNzoId`, namespace moves to `FunkArr.DownloadClient.Pipeline`
- `download-request-tracker`: Messages use `IShardedMessage` via `IWithNzoId`, inline extractor removed, namespace moves to `FunkArr.DownloadClient.Tracker`

## Impact

- All files in `Search/` and `DownloadClient/` move to subfolders with new namespaces
- `using` statements across the codebase (Api controllers, tests, configuration) need updating
- Actor system setup registration changes extractor types
- `MovieSearchPipeline.Search` constructor changes (callers in `NewznabController` affected)
- No persistence impact (messages are not persisted, only events/snapshots are)
