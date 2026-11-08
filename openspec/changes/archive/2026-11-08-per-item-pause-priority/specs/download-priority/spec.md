## MODIFIED Requirements

### Requirement: DownloadPriority enum
The system SHALL define a `DownloadPriority` enum in `FunkArr.Messages.Download` with values `Paused = -2`, `Low = -1`, `Normal = 0`, `High = 1`. The integer values SHALL match SABnzbd's priority scale for direct mapping.

#### Scenario: Enum values
- **WHEN** the DownloadPriority enum is inspected
- **THEN** it SHALL contain `Paused` (-2), `Low` (-1), `Normal` (0), `High` (1)

#### Scenario: Default value
- **WHEN** a DownloadPriority is default-initialized
- **THEN** its value SHALL be `Normal` (0)

### Requirement: SABnzbd priority mapping
The SABnzbd API SHALL map integer priority values to the DownloadPriority enum, ForceStartDownload action, or per-item pause.

#### Scenario: Pause priority mapping
- **WHEN** SABnzbd API receives priority value -2
- **THEN** it SHALL map to `DownloadPriority.Paused`

#### Scenario: Standard priority mapping
- **WHEN** SABnzbd API receives priority value -1
- **THEN** it SHALL map to `DownloadPriority.Low`
- **WHEN** SABnzbd API receives priority value 0
- **THEN** it SHALL map to `DownloadPriority.Normal`
- **WHEN** SABnzbd API receives priority value 1
- **THEN** it SHALL map to `DownloadPriority.High`

#### Scenario: Force priority mapping
- **WHEN** SABnzbd API receives priority value 2 for an existing queued download
- **THEN** it SHALL trigger `ForceStartDownload` instead of setting a queue priority

#### Scenario: Force priority on addfile
- **WHEN** SABnzbd API receives an addfile request with priority value 2
- **THEN** it SHALL enqueue with `DownloadPriority.High` and then trigger `ForceStartDownload`

#### Scenario: Null or missing priority
- **WHEN** SABnzbd API receives no priority value
- **THEN** it SHALL default to `DownloadPriority.Normal`

### Requirement: SABnzbd queue priority operation
The SABnzbd API SHALL support `mode=queue&name=priority&value={nzo_id}&value2={priority_int}` to change a queued download's priority, including pause via -2.

#### Scenario: Set priority via SABnzbd API
- **WHEN** GET `/download/api?mode=queue&name=priority&value={id}&value2=1`
- **THEN** the download's priority SHALL be changed to High

#### Scenario: Pause via SABnzbd priority
- **WHEN** GET `/download/api?mode=queue&name=priority&value={id}&value2=-2`
- **THEN** the download's priority SHALL be changed to Paused
- **AND** the download SHALL NOT be dispatched until priority is changed to a non-paused value

#### Scenario: Resume via SABnzbd priority
- **WHEN** GET `/download/api?mode=queue&name=priority&value={id}&value2=0`
- **AND** the download is currently paused (priority -2)
- **THEN** the download's priority SHALL be changed to Normal
- **AND** the download SHALL be eligible for dispatch

#### Scenario: Force via SABnzbd priority
- **WHEN** GET `/download/api?mode=queue&name=priority&value={id}&value2=2`
- **THEN** `ForceStartDownload` SHALL be triggered for that download

## ADDED Requirements

### Requirement: DispatchNext skips paused items
The DownloadManager's `DispatchNext()` SHALL skip queue entries with `Priority == Paused` when selecting the next download to dispatch.

#### Scenario: Paused item skipped during dispatch
- **WHEN** `DispatchNext()` is called
- **AND** the first queued item has `Priority == Paused`
- **AND** the second queued item has `Priority == Normal`
- **THEN** the second item SHALL be dispatched
- **AND** the paused item SHALL remain in the queue

#### Scenario: All items paused
- **WHEN** `DispatchNext()` is called
- **AND** all queued items have `Priority == Paused`
- **THEN** no download SHALL be dispatched

#### Scenario: Paused item resumes after priority change
- **WHEN** a queued item has `Priority == Paused`
- **AND** its priority is changed to `Normal`
- **THEN** `DispatchNext()` SHALL consider it for dispatch on the next cycle

### Requirement: QueueSlot reports per-item paused status
The SABnzbd API `QueueSlot` response SHALL include a `status` field that reflects per-item pause state.

#### Scenario: Paused item in queue response
- **WHEN** a download with `Priority == Paused` appears in the SABnzbd queue response
- **THEN** its `status` field SHALL be `"Paused"`
- **AND** its `priority` field SHALL be `"Low"` (SABnzbd convention for paused items)

#### Scenario: Non-paused item in queue response
- **WHEN** a download with `Priority != Paused` appears in the SABnzbd queue response
- **THEN** its `status` field SHALL reflect the actual download status (e.g., `"Queued"`, `"Downloading"`)

### Requirement: Addfile with pause priority
The SABnzbd API `addfile` handler SHALL accept priority -2 to enqueue a download in paused state.

#### Scenario: Addfile with priority -2
- **WHEN** POST `/download/api?mode=addfile&priority=-2` with an NZB file
- **THEN** the download SHALL be enqueued with `DownloadPriority.Paused`
- **AND** the download SHALL NOT be dispatched until priority is changed

### Requirement: Persistence round-trip for Paused priority
The `PersistedDownloadPriority` enum SHALL include `Paused = -2` and the enum mapping SHALL round-trip correctly.

#### Scenario: Persist paused priority
- **WHEN** a download with `DownloadPriority.Paused` is persisted
- **THEN** the stored `PersistedDownloadPriority` value SHALL be -2

#### Scenario: Recover paused priority
- **WHEN** recovery loads a persisted download with `PersistedDownloadPriority` value -2
- **THEN** the restored state SHALL have `DownloadPriority.Paused`

### Requirement: Internal API pause and resume endpoints
The internal API SHALL provide endpoints to pause and resume individual downloads.

#### Scenario: Pause a queued download
- **WHEN** POST `/api/downloads/{id}/pause`
- **AND** the download is queued with a non-paused priority
- **THEN** the download's priority SHALL be set to `Paused`
- **AND** the response SHALL be 200 OK

#### Scenario: Resume a paused download
- **WHEN** POST `/api/downloads/{id}/resume`
- **AND** the download is paused
- **THEN** the download's priority SHALL be set to `Normal`
- **AND** the response SHALL be 200 OK

#### Scenario: Pause an already-downloading item
- **WHEN** POST `/api/downloads/{id}/pause`
- **AND** the download is already dispatched (FFmpeg running)
- **THEN** the priority SHALL be updated to `Paused` in state
- **AND** the running download SHALL continue (not cancelled)
- **AND** the response SHALL be 200 OK
