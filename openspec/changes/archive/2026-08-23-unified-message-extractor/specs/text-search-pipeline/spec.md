## MODIFIED Requirements

### Requirement: Shard entity identity

TextSearchPipeline SHALL be a Cluster Sharding entity with `query` as the entity key. Each unique query string MUST map to exactly one entity instance. The `Search` message SHALL implement `IShardedMessage` with `EntityKey => Query`. The entity SHALL use `ShardedMessageExtractor` instead of a dedicated `TextSearchPipelineMessageExtractor`. The namespace SHALL be `FunkArr.Search.Pipelines`.

#### Scenario: Entity activation by query
- **WHEN** a `TextSearchPipeline.Search` message with `Query = "tagesschau"` arrives
- **THEN** the shard system SHALL extract entity key `"tagesschau"` via `IShardedMessage.EntityKey` and route to the correct entity

#### Scenario: Search implements IShardedMessage
- **WHEN** `TextSearchPipeline.Search` is created with `Query = "tatort"`
- **THEN** its `EntityKey` property SHALL return `"tatort"`
