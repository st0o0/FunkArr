## ADDED Requirements

### Requirement: FfmpegRunner is a static facade
The FfmpegRunner SHALL be a static class that encapsulates FFmpeg process lifecycle, progress parsing, and result reporting behind a single `Run` method.

#### Scenario: Run signature
- **WHEN** a caller invokes `FfmpegRunner.Run`
- **THEN** it SHALL accept `IActorRef self`, `string videoUrl`, `string? subtitleUrl`, `string outputPath`
- **AND** return a `CancellationTokenSource` for cancellation control

### Requirement: FfmpegRunner sends ProgressUpdate messages
The FfmpegRunner SHALL parse FFmpeg's progress output and send `ProgressUpdate` messages to the provided `IActorRef` for each complete progress block.

#### Scenario: Progress block parsed
- **WHEN** FFmpeg emits a complete progress block containing `out_time_us`, `total_size`, and `speed`
- **THEN** the runner SHALL send a `ProgressUpdate(TotalSize, OutTimeUs, Speed)` to the caller via `self.Tell`

#### Scenario: Incomplete progress block
- **WHEN** FFmpeg emits lines that do not form a complete progress block
- **THEN** the runner SHALL accumulate them without sending a message

### Requirement: FfmpegRunner sends ProcessExited on completion
The FfmpegRunner SHALL send a `ProcessExited` message to the provided `IActorRef` when the FFmpeg process terminates.

#### Scenario: Process exits successfully
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the runner SHALL send `ProcessExited(ExitCode: 0, ErrorOutput: null, ElapsedSeconds)` to the caller

#### Scenario: Process exits with error
- **WHEN** the FFmpeg process exits with a non-zero code
- **THEN** the runner SHALL send `ProcessExited(ExitCode, ErrorOutput: <stderr>, ElapsedSeconds)` to the caller

#### Scenario: Process fails to start
- **WHEN** the FFmpeg process cannot be spawned
- **THEN** the runner SHALL send `ProcessExited(ExitCode: -1, ErrorOutput: <exception message>, ElapsedSeconds: 0)` to the caller

### Requirement: FfmpegRunner supports cancellation
The FfmpegRunner SHALL observe the returned CancellationTokenSource for cancellation and kill the FFmpeg process when cancelled.

#### Scenario: Cancellation during download
- **WHEN** the CancellationTokenSource is cancelled while FFmpeg is running
- **THEN** the runner SHALL kill the FFmpeg process
- **AND** send `ProcessExited(ExitCode: -1, ErrorOutput: "Cancelled", ElapsedSeconds)` to the caller

### Requirement: ProgressUpdate message shape
The `ProgressUpdate` record SHALL be a flat internal record with three fields: `TotalSize` (long), `OutTimeUs` (long), `Speed` (double).

#### Scenario: Message fields
- **WHEN** a ProgressUpdate is constructed
- **THEN** it SHALL contain `TotalSize`, `OutTimeUs`, and `Speed` without any nested records

### Requirement: ProcessExited message shape
The `ProcessExited` record SHALL be an internal record with three fields: `ExitCode` (int), `ErrorOutput` (string?, nullable), `ElapsedSeconds` (int).

#### Scenario: Message fields
- **WHEN** a ProcessExited is constructed
- **THEN** it SHALL contain `ExitCode`, `ErrorOutput` (nullable), and `ElapsedSeconds`
