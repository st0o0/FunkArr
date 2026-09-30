# Download Queue Reorder (Delta)

## MODIFIED Requirements

### Requirement: MoveDownload command
The system SHALL define a `MoveDownload(Guid DownloadId, int Position, DownloadPriority? Priority = null)` command in `FunkArr.Messages.Download`. It SHALL implement `IWithDownloadId`. When `Priority` is provided, the item's priority SHALL be changed before the position move, atomically.

#### Scenario: Move within same bucket
- **WHEN** MoveDownload is sent with a position within the item's priority bucket and no Priority
- **THEN** the item SHALL be repositioned to that index in the queue
- **AND** a `MoveDownloadCompleted` response SHALL be returned

#### Scenario: Move clamped to bucket boundary
- **WHEN** MoveDownload is sent with a position outside the item's priority bucket
- **THEN** the position SHALL be clamped to the item's bucket boundaries
- **AND** the item SHALL be moved to the clamped position

#### Scenario: Move with priority change (cross-bucket)
- **WHEN** MoveDownload is sent with `Priority = High` for a Normal item at position 0
- **THEN** the item's priority SHALL change to High
- **AND** the item SHALL be moved to position 0 (clamped to High bucket)
- **AND** both operations SHALL be persisted atomically

#### Scenario: Move non-existent item
- **WHEN** MoveDownload is sent for an ID not in the queue
- **THEN** a `MoveDownloadFailed` response SHALL be returned

### Requirement: Internal API move endpoint
The system SHALL expose `POST /api/downloads/queue/{id:guid}/move` accepting a JSON body with `position` (int, required) and `priority` (string, optional: "High"/"Normal"/"Low"). It SHALL Ask the DownloadManager with `MoveDownload` and return the response.

#### Scenario: Move with priority
- **WHEN** POST to `/api/downloads/queue/{id}/move` with `{ "position": 0, "priority": "High" }`
- **THEN** the download SHALL be moved with priority change and 200 OK returned

#### Scenario: Move without priority
- **WHEN** POST to `/api/downloads/queue/{id}/move` with `{ "position": 2 }`
- **THEN** the download SHALL be moved within its current bucket and 200 OK returned
