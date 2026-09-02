## Purpose

Shard routing contracts: entity ID marker interfaces in Messages and a centralized message extractor in Core for Akka.NET Cluster Sharding.

## Requirements

### Requirement: Entity ID interfaces in Messages

FunkArr.Messages SHALL define entity ID marker interfaces following the pattern `IWith{EntityType}Id` with a strongly-typed property. Messages implementing shard routing interfaces SHALL NOT implement additional unrelated marker interfaces. Each sharded message record SHALL implement exactly the `IWith*Id` interface corresponding to its shard type.

#### Scenario: TvSearchCommand implements only IWithSearchId

- **WHEN** `TvSearchCommand` is defined
- **THEN** it SHALL implement `IWithSearchId` and no other marker interface

#### Scenario: MovieSearchCommand implements only IWithSearchId

- **WHEN** `MovieSearchCommand` is defined
- **THEN** it SHALL implement `IWithSearchId` and no other marker interface

#### Scenario: Download message carries entity ID

- **WHEN** a sealed record implements `IWithDownloadId`
- **THEN** it exposes a `Guid DownloadId` property usable for shard routing

#### Scenario: Messages project has no Akka dependency

- **WHEN** FunkArr.Messages is built
- **THEN** it has zero NuGet package references and zero project references

### Requirement: ShardMessageExtractor in Core
FunkArr.Core SHALL define `ShardMessageExtractor` inheriting from
`Akka.Cluster.Sharding.HashCodeMessageExtractor`. The `EntityId(object message)`
override SHALL use a switch expression to pattern-match on `IWith*Id` interfaces
and return the Guid as a string. Unrecognized messages SHALL throw
`ArgumentException`.

#### Scenario: Download message extracts entity ID
- **WHEN** `EntityId()` is called with a message implementing `IWithDownloadId`
- **THEN** the return value equals `message.DownloadId.ToString()`

#### Scenario: Unknown message type throws
- **WHEN** `EntityId()` is called with a message not implementing any `IWith*Id` interface
- **THEN** an `ArgumentException` is thrown

### Requirement: ShardMessageExtractor configurable shard count
`ShardMessageExtractor` SHALL accept a `maxShards` constructor parameter (default 25).
The shard count determines how many shards the hash is distributed across.

#### Scenario: Default shard count
- **WHEN** `new ShardMessageExtractor()` is created without arguments
- **THEN** it uses 25 max shards

#### Scenario: Custom shard count
- **WHEN** `new ShardMessageExtractor(50)` is created
- **THEN** it uses 50 max shards
