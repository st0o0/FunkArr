## MODIFIED Requirements

### Requirement: ShardMessageExtractor in Core

FunkArr.Core SHALL define `ShardMessageExtractor` inheriting from `Akka.Cluster.Sharding.HashCodeMessageExtractor`. The `EntityId(object message)` override SHALL use a switch expression to pattern-match on `IWithEntityId` and return the `EntityId` string. Unrecognized messages SHALL throw `ArgumentException`.

#### Scenario: Any IWithEntityId message extracts entity ID

- **WHEN** `EntityId()` is called with a message implementing any `IWith*Id` interface (which inherits `IWithEntityId`)
- **THEN** the return value equals `((IWithEntityId)message).EntityId`

#### Scenario: Download message extracts entity ID

- **WHEN** `EntityId()` is called with a message implementing `IWithDownloadId`
- **THEN** the return value equals `message.DownloadId.ToString()`

#### Scenario: History message extracts entity ID

- **WHEN** `EntityId()` is called with a message implementing `IWithHistoryId`
- **THEN** the return value equals `message.HistoryId`

#### Scenario: Unknown message type throws

- **WHEN** `EntityId()` is called with a message not implementing `IWithEntityId`
- **THEN** an `ArgumentException` is thrown
