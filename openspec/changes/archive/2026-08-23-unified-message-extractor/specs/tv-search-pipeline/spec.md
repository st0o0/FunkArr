## MODIFIED Requirements

### Requirement: Shard entity identity

TvSearchPipeline SHALL be a Cluster Sharding entity with `tvdbId` as the entity key. Each unique `tvdbId` MUST map to exactly one entity instance. The `Search` message SHALL implement `IShardedMessage` with `EntityKey => TvdbId.ToString()`. The entity SHALL use `ShardedMessageExtractor` instead of a dedicated `TvSearchPipelineMessageExtractor`. The namespace SHALL be `FunkArr.Search.Pipelines`.

#### Scenario: Entity activation by tvdbId
- **WHEN** a `TvSearchPipeline.Search` message with `TvdbId = 12345` arrives
- **THEN** the shard system SHALL extract entity key `"12345"` via `IShardedMessage.EntityKey` and route to the correct entity

#### Scenario: Search implements IShardedMessage
- **WHEN** `TvSearchPipeline.Search` is created with `TvdbId = 42`
- **THEN** its `EntityKey` property SHALL return `"42"`
