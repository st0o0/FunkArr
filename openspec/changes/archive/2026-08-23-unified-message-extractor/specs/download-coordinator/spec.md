## MODIFIED Requirements

### Requirement: DownloadCoordinator ShardRegion entity
The system SHALL register a `DownloadCoordinator` ShardRegion using `ShardedMessageExtractor` with `maxNumberOfShards: 10`. Each entity SHALL be identified by nzoId via `IWithNzoId` which extends `IShardedMessage`. The dedicated `DownloadCoordinatorMessageExtractor` class SHALL be removed. The namespace SHALL be `FunkArr.DownloadClient.Pipeline`.

#### Scenario: Shard region uses unified extractor
- **WHEN** the application starts and registers the DownloadCoordinator ShardRegion
- **THEN** it SHALL use `new ShardedMessageExtractor(10)` for entity ID extraction

#### Scenario: Messages route via IShardedMessage
- **WHEN** a `StartDownload` message with `NzoId = "dl-001"` is sent to the ShardRegion
- **THEN** `IShardedMessage.EntityKey` SHALL return `"dl-001"` (delegating to `IWithNzoId.NzoId`)
