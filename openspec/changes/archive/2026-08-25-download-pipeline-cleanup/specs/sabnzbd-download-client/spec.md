## MODIFIED Requirements

### Requirement: History delete
The system SHALL respond to `GET /download/api?mode=history&name=delete&value={nzoId}` by removing the nzoId from history via `HistoryActor.RemoveFromHistory`. The response SHALL be `{ "status": true }`.

#### Scenario: Delete completed download from history
- **WHEN** a client sends `mode=history&name=delete&value=abc123` and `abc123` is in history
- **THEN** the system SHALL tell `HistoryActor.RemoveFromHistory("abc123")` and return `{ "status": true }`

#### Scenario: Delete non-existent nzoId from history
- **WHEN** a client sends `mode=history&name=delete&value=unknown`
- **THEN** the system SHALL tell `HistoryActor.RemoveFromHistory("unknown")` (which silently ignores) and return `{ "status": true }`

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a typed `SabnzbdQueueResponse` JSON object. Queue slots SHALL include real progress values from DownloadRequestActor.

#### Scenario: Active downloads in queue with progress
- **WHEN** there are active downloads and a client requests `mode=queue`
- **THEN** the system returns a `SabnzbdQueueResponse` with `queue.slots` containing entries with `nzo_id`, `filename`, `status`, `percentage` (from tracker progress), `mb` (from tracker), `mbleft` (from tracker), `timeleft`, `cat`, `index`, `priority`, and `storage`

#### Scenario: Empty queue
- **WHEN** there are no active downloads
- **THEN** the system returns a typed `SabnzbdQueueResponse` with `queue.slots` as an empty array

#### Scenario: Queue slot index and priority fields
- **WHEN** there are 3 items in the queue and a client requests `mode=queue`
- **THEN** each slot SHALL include `index` (0-based position in the array) and `priority` (`"Normal"`)

### Requirement: SABnzbd history reads from HistoryActor
The SABnzbd controller SHALL query `HistoryActor.GetHistory()` for the complete history list instead of fan-out to individual DownloadRequestActor entities.

#### Scenario: History response built from HistoryActor
- **WHEN** `mode=history` is requested
- **THEN** the controller SHALL ask HistoryActor for the full history list and assemble the SABnzbd history JSON from the response

### Requirement: Retry failed download
The system SHALL respond to `GET /download/api?mode=retry&value={nzoId}` by querying `HistoryActor.GetRetryInfo(nzoId)` for retry parameters, then re-enqueueing via `QueueActor.Enqueue`. The response SHALL include the new nzoId.

#### Scenario: Retry a failed download
- **WHEN** a client sends `mode=retry&value=abc123` and `abc123` is a failed download
- **THEN** the system SHALL ask `HistoryActor.GetRetryInfo("abc123")`, enqueue via `QueueActor.Enqueue`, and return `{ "status": true, "nzo_ids": ["<new-nzo-id>"] }`

#### Scenario: Retry unknown nzoId
- **WHEN** a client sends `mode=retry&value=unknown` and HistoryActor replies with `RetryNotFound`
- **THEN** the system SHALL return `{ "status": false, "error": "Job not found in history" }`

#### Scenario: Retry a non-failed download
- **WHEN** a client sends `mode=retry&value=abc123` and `abc123` is in "Completed" status
- **THEN** the system SHALL return `{ "status": false, "error": "Job is not in failed state" }`
