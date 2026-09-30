## MODIFIED Requirements

### Requirement: Stream supervision decider
The system SHALL provide a `StreamSupervision` utility that returns an Akka.Streams supervision decider. The decider MUST log every exception at Warning level before returning the directive. The decider MUST handle failure modes from all pipeline stages including HLS downloads and ffprobe calls.

#### Scenario: Transient cancellation errors resume the stream
- **WHEN** a `TaskCanceledException` or `OperationCanceledException` occurs in a stream stage
- **THEN** the supervision decider returns `Resume` and the element is dropped without stopping the stream

#### Scenario: Programming errors stop the stream
- **WHEN** an unexpected exception (e.g., `NullReferenceException`, `InvalidOperationException`) occurs in a stream stage
- **THEN** the supervision decider returns `Stop` and the stream terminates, triggering actor-level recovery

#### Scenario: FFmpeg process errors resume the stream
- **WHEN** an exception related to FFmpeg process management (e.g., `Win32Exception` from Process.Start, process killed timeout) occurs in the HLS download or subtitle extraction stage
- **THEN** the supervision decider returns `Resume` because the typed outcome already captured the failure, and other pipeline elements should continue

#### Scenario: HTTP errors in subtitle stage resume the stream
- **WHEN** an `HttpRequestException` occurs in the subtitle acquisition stage
- **THEN** the supervision decider returns `Resume` because missing subtitles are non-fatal
