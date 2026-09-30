## MODIFIED Requirements

### Requirement: Single-node Cluster Sharding infrastructure
The system SHALL configure Akka.Cluster with single-node seed and register a DownloadRequestTracker ShardRegion using `ShardedMessageExtractor` with `maxNumberOfShards: 10`. Entity IDs SHALL be nzoIds extracted from messages via `IWithNzoId` which extends `IShardedMessage`. The inline `DownloadRequestTrackerMessageExtractor` class SHALL be removed. The namespace SHALL be `FunkArr.DownloadClient.Tracker`.

#### Scenario: Cluster starts on single node
- **WHEN** the application starts
- **THEN** the ActorSystem SHALL join a single-node cluster and the DownloadRequestTracker ShardRegion SHALL use `new ShardedMessageExtractor(10)`

#### Scenario: Entity addressed by nzoId via IShardedMessage
- **WHEN** a message implementing `IWithNzoId` with `NzoId = "abc123"` is sent to the ShardRegion
- **THEN** `IShardedMessage.EntityKey` SHALL return `"abc123"` and the message SHALL be routed to entity `"abc123"`
