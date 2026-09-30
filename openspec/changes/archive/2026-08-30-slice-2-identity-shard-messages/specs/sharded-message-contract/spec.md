## REMOVED Requirements

### Requirement: Generic sharded message interface
**Reason**: Replaced by per-family typed shard interfaces (`ISeriesShard`, `IMovieShard`, `IDownloadShard`, `ISearchShard`) that provide compile-time routing safety. The generic `IShardedMessage` with `string EntityKey` allowed any message to be sent to any region.
**Migration**: Messages implement their family's shard interface. `EntityKey` property replaced by typed `Id`. `ShardedMessageExtractor` replaced by per-region extractors.

#### Scenario: IShardedMessage no longer exists
- **WHEN** code attempts to reference `IShardedMessage`
- **THEN** it SHALL fail to compile — the interface has been removed
