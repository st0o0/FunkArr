## Purpose

Akka.Streams-based download pipeline with concurrent HTTP downloads, backpressure-aware muxing, and persistent state via Akka.Persistence + SQLite.

## Requirements

### Requirement: Concurrent download workers
The system SHALL execute downloads concurrently up to a configurable maximum (default 20) using an Akka.Streams pipeline materialized inside the DownloadQueueActor. Downloads MUST run as `SelectAsyncUnordered` stages — not as individual worker actors.

#### Scenario: Concurrency limit respected
- **WHEN** 25 downloads are queued and the concurrency limit is 20
- **THEN** 20 downloads run simultaneously inside the stream pipeline and 5 jobs remain in "Queued" status waiting for backpressure to release

#### Scenario: Worker completes and next job starts
- **WHEN** a download completes inside the stream pipeline and there are queued jobs remaining
- **THEN** the DownloadQueueActor offers the next queued job to the Source.Queue and the stream picks it up for processing

#### Scenario: Backpressure from muxing stage
- **WHEN** the mux stage (parallelism 4) is saturated and more downloads complete
- **THEN** the stream applies backpressure and completed downloads wait in the buffer until a mux slot opens

### Requirement: HTTP stream download
Each download stage element SHALL download content via HTTP GET with chunked reading and progress tracking. The download lambda MUST report progress to the DownloadQueueActor via `Self.Tell`.

#### Scenario: Successful download with progress
- **WHEN** a download processes a 500MB video file in the stream pipeline
- **THEN** the lambda reports `ProgressUpdate` messages to the actor via `Self.Tell` every 2 seconds, and the file is written to the configured temp directory

#### Scenario: Download fails with transient HTTP error
- **WHEN** a download fails with an `HttpRequestException`
- **THEN** the lambda catches the error and returns a `DownloadOutcome.Failure`, the actor persists a `DownloadFailed` event, and the stream continues processing other downloads

### Requirement: Supervision and fault isolation
The DownloadQueueActor SHALL be supervised by a `BackoffSupervisor`. A failing download MUST NOT affect other downloads in the stream pipeline.

#### Scenario: One download fails, others continue
- **WHEN** a download for job-2 fails with a 403 Forbidden error
- **THEN** the lambda returns `DownloadOutcome.Failure` for job-2, all other downloads continue unaffected, and the actor marks job-2 as "Failed"

#### Scenario: Stream-level crash triggers actor restart
- **WHEN** the stream terminates due to an unexpected error (supervision Stop)
- **THEN** the BackoffSupervisor restarts the DownloadQueueActor, events are replayed, and a new stream is materialized. Jobs in `Downloading` or `Muxing` state are reset to `Queued`

### Requirement: Queue persistence
The DownloadQueueActor SHALL persist its state using Akka.Persistence with SQLite backend. Queue state MUST survive application restarts.

#### Scenario: Queue recovery after restart
- **WHEN** the application restarts with 3 queued and 1 in-progress download
- **THEN** the DownloadQueueActor recovers its state from the journal, resets the in-progress download to "Queued", materializes the stream, and re-pushes all queued jobs into the Source.Queue

#### Scenario: History survives restart
- **WHEN** the application restarts and there are 5 completed downloads in history
- **THEN** the history endpoint returns all 5 completed entries with their original metadata

### Requirement: Download output organization
The system SHALL write completed downloads to the configured output directory using the title from the download request as the filename.

#### Scenario: Completed download file placement
- **WHEN** a download for "Show.S01E03.GERMAN.1080p.WEB.h264-FA" completes and muxing finishes
- **THEN** the final MKV file is placed at `<output_dir>/Show.S01E03.GERMAN.1080p.WEB.h264-FA/Show.S01E03.GERMAN.1080p.WEB.h264-FA.mkv`

### Requirement: Source.Queue as actor-to-stream bridge
The DownloadQueueActor SHALL use `Source.Queue<DownloadRequest>` to bridge between the actor mailbox and the stream pipeline. New jobs MUST be offered to the queue via `OfferAsync`, with the result piped back to the actor.

#### Scenario: Job offered to running stream
- **WHEN** a new download is enqueued and the actor is in `Ready` state
- **THEN** the actor calls `queue.OfferAsync(request)` and the download enters the stream pipeline

#### Scenario: Queue backpressure when pipeline is saturated
- **WHEN** the Source.Queue buffer (64 elements) is full because all download slots and mux slots are occupied
- **THEN** `OfferAsync` returns a pending Task, the actor remains responsive to status queries, and the offer completes when a slot opens

### Requirement: Configurable per-stage parallelism
The system SHALL support separate parallelism configuration for download and mux stages.

#### Scenario: Default parallelism values
- **WHEN** no configuration override is provided
- **THEN** the download stage runs with parallelism 20 and the mux stage runs with parallelism 4

#### Scenario: Custom parallelism via configuration
- **WHEN** `FunkArr__ConcurrentDownloads=10` and `FunkArr__MuxConcurrency=2` are set
- **THEN** the stream pipeline uses `SelectAsyncUnordered(10)` for downloads and `SelectAsyncUnordered(2)` for muxing
