## MODIFIED Requirements

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
