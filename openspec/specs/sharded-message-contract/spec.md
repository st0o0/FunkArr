# Sharded Message Contract

## Purpose

Defines the unified message extraction contract for Cluster Sharding. A single `IShardedMessage` interface and `ShardedMessageExtractor` replace all per-entity message extractors, ensuring consistent entity ID extraction across all shard regions.

## Requirements

### Requirement: IShardedMessage interface

The system SHALL define an `IShardedMessage` interface in `FunkArr.Shared` with a single `string EntityKey` property. All message types routed through Cluster Sharding MUST implement this interface.

#### Scenario: Interface definition
- **WHEN** a developer creates a new sharded message type
- **THEN** they SHALL implement `IShardedMessage` with a computed `EntityKey` property that returns the shard entity identifier

#### Scenario: IWithNzoId extends IShardedMessage
- **WHEN** `IWithNzoId` is defined
- **THEN** it SHALL extend `IShardedMessage` and provide a default interface method implementation mapping `EntityKey` to `NzoId`

#### Scenario: Existing IWithNzoId implementors unchanged
- **WHEN** a record implements `IWithNzoId` with a `NzoId` property
- **THEN** it SHALL automatically satisfy `IShardedMessage` without code changes

### Requirement: ShardedMessageExtractor

The system SHALL provide a single `ShardedMessageExtractor` class in `FunkArr.Shared` that extracts entity IDs from any `IShardedMessage`. It SHALL accept `maxNumberOfShards` as a constructor parameter.

#### Scenario: Entity ID extraction from IShardedMessage
- **WHEN** a message implementing `IShardedMessage` with `EntityKey = "abc123"` is processed by the extractor
- **THEN** the extractor SHALL return `"abc123"` as the entity ID

#### Scenario: Non-sharded message returns null
- **WHEN** a message that does not implement `IShardedMessage` is processed by the extractor
- **THEN** the extractor SHALL return `null`

#### Scenario: Different shard counts per region
- **WHEN** `ShardedMessageExtractor` is instantiated with `maxNumberOfShards: 20` for search and `maxNumberOfShards: 10` for downloads
- **THEN** each instance SHALL use its own shard count for hash distribution

### Requirement: Per-entity extractors removed

All per-entity `MessageExtractor` classes SHALL be deleted: `TvSearchActorMessageExtractor`, `MovieSearchActorMessageExtractor`, `TextSearchActorMessageExtractor`, `DownloadActorMessageExtractor`, and the inline `DownloadRequestActorMessageExtractor`.

#### Scenario: No per-entity extractor classes exist
- **WHEN** the codebase is searched for classes extending `HashCodeMessageExtractor`
- **THEN** only `ShardedMessageExtractor` SHALL exist

### Requirement: MovieSearchActor.Search derives EntityKey

`MovieSearchActor.Search` SHALL accept `ImdbId` and `Query` parameters and compute `EntityKey` as `ImdbId ?? $"q:{Query}"`. It SHALL NOT accept an `EntityKey` constructor parameter.

#### Scenario: Search with IMDb ID
- **WHEN** `MovieSearchActor.Search` is created with `ImdbId = "tt1234567"` and `Query = null`
- **THEN** `EntityKey` SHALL be `"tt1234567"`

#### Scenario: Search with query fallback
- **WHEN** `MovieSearchActor.Search` is created with `ImdbId = null` and `Query = "some movie"`
- **THEN** `EntityKey` SHALL be `"q:some movie"`
