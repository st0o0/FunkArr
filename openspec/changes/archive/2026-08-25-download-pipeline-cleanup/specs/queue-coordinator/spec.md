## ADDED Requirements

### Requirement: Snapshot support
QueueActor SHALL save a snapshot every 50 persisted events. On recovery, it SHALL load the latest snapshot and replay only subsequent events. The snapshot SHALL contain the queue (ordered list of QueueEntry) and active set (HashSet of nzoIds).

#### Scenario: Snapshot saved after threshold
- **WHEN** the 50th event is persisted since the last snapshot
- **THEN** QueueActor SHALL save a snapshot of its current queue and active state

#### Scenario: Recovery from snapshot
- **WHEN** QueueActor restarts with a snapshot at sequence 50 and 10 subsequent events
- **THEN** it SHALL load the snapshot, replay 10 events, requeue active entries, and call `TryStartNext()`

#### Scenario: Recovery without snapshot
- **WHEN** QueueActor restarts with no snapshot
- **THEN** it SHALL replay all events from the beginning (existing behavior)

## REMOVED Requirements

### Requirement: History tracking in QueueActor
**Reason**: History responsibility moved to dedicated HistoryActor. QueueActor no longer tracks completed job IDs or handles history removal.
**Migration**: SABnzbd controller queries HistoryActor.GetHistory instead of QueueActor.GetCompletedJobIds + fan-out. History delete routes to HistoryActor.RemoveFromHistory.

## MODIFIED Requirements

### Requirement: Event-sourced scheduling persistence
`QueueActor` SHALL be a `ReceivePersistentActor` with `PersistenceId: "queue-coordinator"`. It SHALL persist scheduling events: `JobEnqueued`, `JobStarted`, `JobFinished`, `JobRemoved`. Recovery SHALL handle legacy `JobRemovedFromHistory` events by silently ignoring them (backward compatibility during transition). Recovery SHALL load the latest snapshot before replaying events.

#### Scenario: Enqueue persisted
- **WHEN** a new download is enqueued
- **THEN** `QueueActor` SHALL persist a `JobEnqueued` event before acknowledging

#### Scenario: Recovery reconstructs queue
- **WHEN** `QueueActor` restarts after a crash
- **THEN** it SHALL load the latest snapshot (if any), replay subsequent events to reconstruct `_queue` and `_active` sets, reset all `_active` entries back to `_queue`, and call `TryStartNext()`

#### Scenario: Legacy JobRemovedFromHistory event ignored
- **WHEN** recovery encounters a `QueueJobRemovedFromHistory` DTO from a pre-cleanup journal
- **THEN** QueueActor SHALL silently ignore it without error
