## MODIFIED Requirements

### Requirement: Enqueue generates nzoId and replies
`QueueCoordinator` SHALL generate a unique `nzoId` for each enqueued download and reply with the nzoId to the sender. The `Enqueue` message SHALL include an optional `Category` field.

#### Scenario: Enqueue reply with category
- **WHEN** a controller sends `Enqueue(downloadUrl, title, subtitleUrl, category: "tv")`
- **THEN** `QueueCoordinator` SHALL generate a 10-character hex nzoId, persist `JobEnqueued` (including category), and reply with the nzoId

#### Scenario: Enqueue without category
- **WHEN** a controller sends `Enqueue(downloadUrl, title, subtitleUrl, category: null)`
- **THEN** `QueueCoordinator` SHALL persist `JobEnqueued` with null category and reply with the nzoId

### Requirement: Category passed through to DownloadCoordinator
When starting a download, `QueueCoordinator` SHALL pass the stored category to `DownloadCoordinator` via the `StartDownload` message. Category resolution to a filesystem path is NOT done here — it is deferred to `FileService` at mux time.

#### Scenario: Category forwarded on start
- **WHEN** a queued job with category `"tv"` is started
- **THEN** `QueueCoordinator` SHALL pass `category: "tv"` in the `StartDownload` message

### Requirement: Category stored in queue state
`QueueCoordinator` SHALL persist the category in the `JobEnqueued` event and reconstruct it during recovery so it is available when the job is eventually started.

#### Scenario: Category survives recovery
- **WHEN** `QueueCoordinator` restarts and replays a `JobEnqueued` event with category `"movies"`
- **THEN** the recovered queue entry SHALL retain category `"movies"`

### Requirement: GetQueueOrder returns category
`QueueCoordinator` SHALL include the category in the queue order response so controllers can include it in API responses.

#### Scenario: Queue order includes category
- **WHEN** `GetQueueOrder` is received with jobs that have categories
- **THEN** the response SHALL include the category for each nzoId
