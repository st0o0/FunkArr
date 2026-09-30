## ADDED Requirements

### Requirement: Download queue management
The system SHALL maintain a queue of pending downloads managed by a DownloadQueueActor. The queue MUST support adding, listing, and removing download jobs.

#### Scenario: New download added to queue
- **WHEN** a download request is received via the SABnzbd addfile endpoint
- **THEN** the DownloadQueueActor creates a new job entry with status "Queued" and assigns a unique `nzo_id`

#### Scenario: Queue reports status
- **WHEN** the SABnzbd queue endpoint is called
- **THEN** the DownloadQueueActor returns all active jobs with their current status, progress percentage, and estimated time remaining

### Requirement: Concurrent download workers
The system SHALL execute downloads concurrently up to a configurable maximum (default 3). Each download MUST run in its own DownloadWorkerActor.

#### Scenario: Concurrency limit respected
- **WHEN** 5 downloads are queued and the concurrency limit is 3
- **THEN** 3 DownloadWorkerActors are running simultaneously and 2 jobs remain in "Queued" status

#### Scenario: Worker completes and next job starts
- **WHEN** a DownloadWorkerActor completes its download and there are queued jobs remaining
- **THEN** the DownloadQueueActor starts a new worker for the next queued job

### Requirement: HTTP stream download
Each DownloadWorkerActor SHALL download content via HTTP GET with chunked reading and progress tracking. The worker MUST report progress to the DownloadQueueActor.

#### Scenario: Successful download with progress
- **WHEN** a worker downloads a 500MB video file
- **THEN** the worker reports progress updates to the queue actor, and the file is written to the configured temp directory

#### Scenario: Download resumes after transient failure
- **WHEN** a download fails with a transient HTTP error (timeout, 5xx)
- **THEN** the worker actor is restarted by its supervisor and retries the download from the beginning (up to 3 retries)

### Requirement: Supervision and fault isolation
The DownloadQueueActor SHALL supervise DownloadWorkerActors using a OneForOneStrategy. A failing worker MUST NOT affect other workers or the queue state.

#### Scenario: One download fails, others continue
- **WHEN** worker-2 crashes due to a 403 Forbidden error and has exhausted retries
- **THEN** worker-1 and worker-3 continue unaffected, and the failed job is moved to history with status "Failed"

#### Scenario: Worker exhausts retries
- **WHEN** a DownloadWorkerActor fails 3 times consecutively
- **THEN** the DownloadQueueActor stops retrying, marks the job as "Failed" in history, and starts the next queued job if any

### Requirement: Queue persistence
The DownloadQueueActor SHALL persist its state using Akka.Persistence with SQLite backend. Queue state MUST survive application restarts.

#### Scenario: Queue recovery after restart
- **WHEN** the application restarts with 3 queued and 1 in-progress download
- **THEN** the DownloadQueueActor recovers its state from the journal, re-queues the in-progress download (reset to "Queued"), and resumes processing

#### Scenario: History survives restart
- **WHEN** the application restarts and there are 5 completed downloads in history
- **THEN** the history endpoint returns all 5 completed entries with their original metadata

### Requirement: Download output organization
The system SHALL write completed downloads to the configured output directory using the title from the download request as the filename.

#### Scenario: Completed download file placement
- **WHEN** a download for "Show.S01E03.GERMAN.1080p.WEB.h264-FA" completes and muxing finishes
- **THEN** the final MKV file is placed at `<output_dir>/Show.S01E03.GERMAN.1080p.WEB.h264-FA/Show.S01E03.GERMAN.1080p.WEB.h264-FA.mkv`
