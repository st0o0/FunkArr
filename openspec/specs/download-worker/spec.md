# Download Worker

## Purpose

Sharded Entity actor that runs FFmpeg, parses progress, and manages per-download lifecycle.

## Requirements

### Requirement: DownloadWorker is a Sharded Entity
The DownloadWorker SHALL be registered as a Sharded Entity with the DownloadId (Guid) as the entity key.

#### Scenario: Shard registration
- **WHEN** the actor system starts
- **THEN** a DownloadWorker shard region SHALL be registered

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` messages by building FFmpeg arguments and spawning the FFmpeg process.

#### Scenario: Start with subtitle
- **WHEN** a StartDownload is received with a non-null SubtitleUrl
- **THEN** the Worker SHALL spawn FFmpeg with both video and subtitle inputs, producing an MKV with a soft-sub SRT track

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received with a null SubtitleUrl
- **THEN** the Worker SHALL spawn FFmpeg with only the video input, producing an MKV without subtitle tracks

#### Scenario: Start with HLS source
- **WHEN** a StartDownload is received with a VideoUrl ending in `.m3u8` or containing `m3u8`
- **THEN** the Worker SHALL spawn FFmpeg with the same arguments as for direct HTTP — FFmpeg handles HLS transparently

### Requirement: DownloadWorker reports progress
The DownloadWorker SHALL parse FFmpeg's `-progress pipe:1` output and send `DownloadProgress` messages to the DownloadManager at regular intervals.

#### Scenario: Progress reporting
- **WHEN** FFmpeg emits a progress block containing `out_time_us`, `total_size`, and `speed`
- **THEN** the Worker SHALL send a DownloadProgress message to the Manager with the parsed values

#### Scenario: Progress interval
- **WHEN** FFmpeg emits progress blocks
- **THEN** the Worker SHALL forward progress to the Manager no more frequently than once per second

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL send `DownloadCompleted` to the Manager when FFmpeg exits with code 0.

#### Scenario: Successful completion
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL send DownloadCompleted with the output file path and elapsed download time
- **AND** the Worker SHALL stop itself (passivate)

### Requirement: DownloadWorker reports failure
The DownloadWorker SHALL send `DownloadFailed` to the Manager when FFmpeg exits with a non-zero code.

#### Scenario: FFmpeg failure
- **WHEN** the FFmpeg process exits with a non-zero code
- **THEN** the Worker SHALL send DownloadFailed with the stderr output as the reason
- **AND** the Worker SHALL stop itself (passivate)

#### Scenario: FFmpeg process crash
- **WHEN** the FFmpeg process cannot be started or is killed
- **THEN** the Worker SHALL send DownloadFailed with an appropriate error message

### Requirement: DownloadWorker subtitle failure is non-fatal
The DownloadWorker SHALL proceed without subtitles if the subtitle URL fails to download or FFmpeg cannot convert the subtitle format.

#### Scenario: Subtitle download fails
- **WHEN** FFmpeg fails due to a subtitle input error
- **THEN** the Worker SHALL retry the FFmpeg command without the subtitle input

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain an explicit state record tracking the current download command and status.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain the StartDownload command, current DownloadStatus, and the OS process ID of the running FFmpeg process (if any)
