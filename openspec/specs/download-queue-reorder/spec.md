# Download Queue Reorder

## Purpose

Queue manipulation operations — move-to-position, swap, and priority-aware state management for the download pipeline.

## Requirements

### Requirement: QueueEntry value type
The system SHALL define a `readonly record struct QueueEntry(Guid Id, DownloadPriority Priority)` in `FunkArr.Download` for representing a queued download with its priority.

#### Scenario: QueueEntry fields
- **WHEN** a QueueEntry is created
- **THEN** it SHALL contain `Id` (Guid) and `Priority` (DownloadPriority)
- **AND** it SHALL be a readonly record struct (value type)

### Requirement: Priority-ordered queue state
The `DownloadManagerState.Queued` property SHALL be `IReadOnlyList<QueueEntry>`. The list SHALL be maintained in priority order: all High entries before Normal, all Normal before Low. Within each priority bucket, order SHALL reflect insertion or move position.

#### Scenario: Queue ordering invariant
- **WHEN** the queue contains entries of mixed priorities
- **THEN** all High-priority entries SHALL appear before all Normal-priority entries
- **AND** all Normal-priority entries SHALL appear before all Low-priority entries

#### Scenario: FIFO within bucket
- **WHEN** two downloads with the same priority are enqueued in sequence
- **THEN** the first enqueued SHALL appear before the second within their priority bucket

### Requirement: Dispatched preserves priority
The `DownloadManagerState.Dispatched` property SHALL be `IReadOnlyDictionary<Guid, DownloadPriority>`. When an item is dispatched, its priority SHALL be looked up from the Queued list and stored in the dictionary.

#### Scenario: Priority preserved through dispatch
- **WHEN** a High-priority item is dispatched
- **THEN** the Dispatched dictionary SHALL store its priority as High

#### Scenario: Recovery reconstructs priority
- **WHEN** `ResetDispatched()` is called during recovery
- **THEN** each dispatched item SHALL be re-inserted into Queued as a QueueEntry with its stored priority
- **AND** the resulting queue SHALL maintain the priority ordering invariant

### Requirement: MoveDownload command
The system SHALL define a `MoveDownload(Guid DownloadId, int Position)` command in `FunkArr.Messages.Download`. It SHALL implement `IWithDownloadId`.

#### Scenario: Move within same bucket
- **WHEN** MoveDownload is sent with a position within the item's priority bucket
- **THEN** the item SHALL be repositioned to that index in the queue
- **AND** a `MoveDownloadCompleted` response SHALL be returned

#### Scenario: Move clamped to bucket boundary
- **WHEN** MoveDownload is sent with a position outside the item's priority bucket (e.g., position 0 for a Normal item when High items exist)
- **THEN** the position SHALL be clamped to the item's bucket boundaries
- **AND** the item SHALL be moved to the clamped position

#### Scenario: Move non-existent item
- **WHEN** MoveDownload is sent for an ID not in the queue
- **THEN** a `MoveDownloadFailed` response SHALL be returned

### Requirement: MoveDownload response
The system SHALL define `abstract record MoveDownloadResponse` with `MoveDownloadCompleted` and `MoveDownloadFailed(string Reason)`.

#### Scenario: Response hierarchy
- **WHEN** a MoveDownload command succeeds
- **THEN** it SHALL return `MoveDownloadCompleted`
- **WHEN** a MoveDownload command fails
- **THEN** it SHALL return `MoveDownloadFailed` with a reason string

### Requirement: SwapDownloads command
The system SHALL define a `SwapDownloads(Guid DownloadId1, Guid DownloadId2)` command in `FunkArr.Messages.Download`.

#### Scenario: Swap within same bucket
- **WHEN** SwapDownloads is sent for two items with the same priority
- **THEN** their positions SHALL be exchanged
- **AND** a `SwapDownloadsCompleted` response SHALL be returned

#### Scenario: Swap across buckets rejected
- **WHEN** SwapDownloads is sent for two items with different priorities
- **THEN** a `SwapDownloadsFailed` response SHALL be returned with reason indicating different priorities

#### Scenario: Swap non-existent item
- **WHEN** SwapDownloads is sent for an ID not in the queue
- **THEN** a `SwapDownloadsFailed` response SHALL be returned

### Requirement: SwapDownloads response
The system SHALL define `abstract record SwapDownloadsResponse` with `SwapDownloadsCompleted` and `SwapDownloadsFailed(string Reason)`.

#### Scenario: Response hierarchy
- **WHEN** a SwapDownloads command succeeds
- **THEN** it SHALL return `SwapDownloadsCompleted`
- **WHEN** a SwapDownloads command fails
- **THEN** it SHALL return `SwapDownloadsFailed` with a reason string

### Requirement: SetDownloadPriority command
The system SHALL define a `SetDownloadPriority(Guid DownloadId, DownloadPriority Priority)` command in `FunkArr.Messages.Download`. It SHALL implement `IWithDownloadId`.

#### Scenario: Priority change re-inserts at end of target bucket
- **WHEN** SetDownloadPriority changes an item from Normal to High
- **THEN** the item SHALL be removed from its current position
- **AND** the item SHALL be inserted at the end of the High-priority bucket

#### Scenario: Same priority is no-op
- **WHEN** SetDownloadPriority is sent with the item's current priority
- **THEN** a `SetDownloadPriorityCompleted` response SHALL be returned
- **AND** the item's position SHALL not change

#### Scenario: Priority change non-existent item
- **WHEN** SetDownloadPriority is sent for an ID not in the queue
- **THEN** a `SetDownloadPriorityFailed` response SHALL be returned

### Requirement: SetDownloadPriority response
The system SHALL define `abstract record SetDownloadPriorityResponse` with `SetDownloadPriorityCompleted` and `SetDownloadPriorityFailed(string Reason)`.

#### Scenario: Response hierarchy
- **WHEN** a SetDownloadPriority command succeeds
- **THEN** it SHALL return `SetDownloadPriorityCompleted`
- **WHEN** a SetDownloadPriority command fails
- **THEN** it SHALL return `SetDownloadPriorityFailed` with a reason string

### Requirement: DownloadMoved persistence event
The system SHALL define a `DownloadMoved(Guid DownloadId, int Position)` persistence DTO in `FunkArr.Persistence.Events.Download`.

#### Scenario: DownloadMoved fields
- **WHEN** a DownloadMoved event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid) and `Position` (int, global queue index)

### Requirement: DownloadSwapped persistence event
The system SHALL define a `DownloadSwapped(Guid DownloadId1, Guid DownloadId2)` persistence DTO in `FunkArr.Persistence.Events.Download`.

#### Scenario: DownloadSwapped fields
- **WHEN** a DownloadSwapped event is persisted
- **THEN** it SHALL contain `DownloadId1` (Guid) and `DownloadId2` (Guid)

### Requirement: DownloadPriorityChanged persistence event
The system SHALL define a `DownloadPriorityChanged(Guid DownloadId, DownloadPriority Priority)` persistence DTO in `FunkArr.Persistence.Events.Download`.

#### Scenario: DownloadPriorityChanged fields
- **WHEN** a DownloadPriorityChanged event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid) and `Priority` (DownloadPriority)

### Requirement: DispatchNext priority ordering
The `DispatchNext()` method SHALL dispatch downloads in queue order, which is pre-sorted by priority. It SHALL NOT contain priority-specific logic — the sort invariant in the Queued list guarantees correct dispatch order.

#### Scenario: High-priority dispatched first
- **WHEN** the queue contains [High:A, Normal:B] and one slot is free
- **THEN** A SHALL be dispatched

#### Scenario: Multiple slots fill in order
- **WHEN** the queue contains [High:A, Normal:B, Low:C] and all slots are free
- **THEN** A, B, C SHALL be dispatched in that order

### Requirement: Internal API move endpoint
The system SHALL expose `POST /api/downloads/queue/{id:guid}/move` accepting a JSON body with `position` (int). It SHALL Ask the DownloadManager with `MoveDownload` and return the response.

#### Scenario: Successful move
- **WHEN** POST to `/api/downloads/queue/{id}/move` with `{ "position": 2 }`
- **THEN** the download SHALL be moved and 200 OK returned

#### Scenario: Invalid ID
- **WHEN** POST to `/api/downloads/queue/{id}/move` with a non-existent ID
- **THEN** 404 Not Found SHALL be returned

### Requirement: Internal API priority endpoint
The system SHALL expose `POST /api/downloads/queue/{id:guid}/priority` accepting a JSON body with `priority` (string: "High", "Normal", or "Low"). It SHALL Ask the DownloadManager with `SetDownloadPriority` and return the response.

#### Scenario: Successful priority change
- **WHEN** POST to `/api/downloads/queue/{id}/priority` with `{ "priority": "High" }`
- **THEN** the download's priority SHALL be changed and 200 OK returned

### Requirement: Internal API swap endpoint
The system SHALL expose `POST /api/downloads/queue/swap` accepting a JSON body with `id1` and `id2` (Guid). It SHALL Ask the DownloadManager with `SwapDownloads` and return the response.

#### Scenario: Successful swap
- **WHEN** POST to `/api/downloads/queue/swap` with two valid IDs of same priority
- **THEN** the downloads SHALL be swapped and 200 OK returned

#### Scenario: Different priorities
- **WHEN** POST to `/api/downloads/queue/swap` with IDs of different priorities
- **THEN** 400 Bad Request SHALL be returned
