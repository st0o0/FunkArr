## Why

The generic `IShardedMessage` with `string EntityKey` provides no compile-time safety — any message can be sent to any shard region, and the entity key format is a string convention enforced nowhere. 34 message records carry `EntityKey = string.Empty` or compute it from their actual id. Entity actors parse `Self.Path.Name` back to their typed id (int, string) with inconsistent error handling. This slice replaces the single generic interface with per-family typed shard interfaces, making message routing a compile-time property.

## What Changes

- **Add** `ISeriesShard { int Id }`, `IMovieShard { string Id }`, `IDownloadShard { string Id }` interfaces in FunkArr.Messages
- **Add** `IRequest<TResponse>` marker interface in FunkArr.Messages
- **Add** per-region shard extractors (`SeriesShardExtractor`, `MovieShardExtractor`, `DownloadShardExtractor`, `SearchShardExtractor`) in FunkArr, replacing the single `ShardedMessageExtractor`
- **Update** 34 message records to implement the new typed shard interfaces instead of `IShardedMessage`
- **Update** entity actor constructors to receive their typed id instead of parsing `Self.Path.Name`
- **Update** shard region configuration to use per-region extractors and pass entity id
- **Update** controllers to construct messages without `EntityKey` set-expressions
- **Move** remaining standalone message files (`SearchMessages.cs`, `SearchCoordinatorMessages.cs`, `DownloadCoordinatorMessages.cs`) to FunkArr.Messages
- **Delete** `IShardedMessage`, `ShardedMessageExtractor`, and `IWithNzoId`
- **BREAKING**: Persistence IDs unchanged — journal data stays compatible

## Capabilities

### New Capabilities

- `shard-identity`: Per-family typed shard interfaces and per-region extractors

### Modified Capabilities

- `sharded-message-contract`: Replace generic IShardedMessage with typed shard interfaces

## Impact

- **Messages:** 34 records updated from `IShardedMessage`/`IWithNzoId` to typed shard interfaces
- **Actors:** ShowActor, MovieActor, DownloadActor, DownloadRequestActor — constructors change
- **Setup:** `FunkArrActorSystemSetup` — 5 shard region registrations updated
- **Controllers:** RulesetController (16), MatchIntelligenceController (2) — message construction simplified
- **Tests:** Actor tests need updated message construction; shard-specific test helpers may be needed
