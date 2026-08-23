## Purpose

FFmpeg process management for remuxing downloaded video + subtitle streams into MKV containers with correct German language metadata. Exposed as a stateless MuxingService for use within the Akka.Streams download pipeline.

## Requirements

### Requirement: Video remuxing to MKV
The MuxingService SHALL remux downloaded video files into MKV containers using FFmpeg with stream-copy (no re-encoding). The output MUST have correct language metadata set to German. MuxingService is a stateless DI-registered service, not an actor. The remux stage SHALL be exposed as a composable stream flow.

#### Scenario: MP4 to MKV remux
- **WHEN** a downloaded MP4 file is passed to the remux stage
- **THEN** FFmpeg runs with `-c copy` flags, produces an MKV file with `language=ger` metadata on video and audio streams, and the original MP4 is deleted

#### Scenario: Remux stage as composable flow
- **WHEN** the remux stage is used in the pipeline graph
- **THEN** it accepts a record containing video path, optional subtitle path, output directory, and title, and produces a `MuxOutcome`

### Requirement: Subtitle merging
The remux stage SHALL merge subtitle files into the MKV container during remuxing when subtitles are available. Subtitle files arriving at the remux stage MUST already be in SRT format (normalization happens in a prior stage).

#### Scenario: Video with SRT subtitle
- **WHEN** a video file and an SRT subtitle file are available for the same content
- **THEN** FFmpeg maps both into the output MKV with the subtitle stream tagged as `language=ger`

#### Scenario: Video without subtitles
- **WHEN** only a video file is available (no subtitle file)
- **THEN** FFmpeg remuxes the video alone into MKV without subtitle streams

### Requirement: FFmpeg process management
The MuxingService SHALL manage FFmpeg as an external process with timeout protection and error detection.

#### Scenario: FFmpeg completes successfully
- **WHEN** FFmpeg exits with code 0
- **THEN** the stage produces a `MuxOutcome.Success` with the output file path

#### Scenario: FFmpeg fails
- **WHEN** FFmpeg exits with a non-zero exit code
- **THEN** the stage produces a `MuxOutcome.Failure` with the error details

#### Scenario: FFmpeg hangs
- **WHEN** FFmpeg does not complete within the configured timeout (default 10 minutes)
- **THEN** the process is killed and the stage produces a `MuxOutcome.Failure`

### Requirement: Temp file cleanup
The MuxingService SHALL clean up temporary files (downloaded source video, intermediate subtitle files) after successful muxing.

#### Scenario: Cleanup after successful mux
- **WHEN** muxing completes successfully
- **THEN** the source MP4 and any intermediate subtitle files are deleted, leaving only the final MKV

#### Scenario: Cleanup after failed mux
- **WHEN** muxing fails
- **THEN** temporary files are preserved for debugging

### Requirement: Parallel mux execution
The download pipeline SHALL run multiple mux operations concurrently via the stream's `SelectAsyncUnordered` stage, using `_options.ConcurrentDownloads` (default 3) as the parallelism value. There is no separate mux concurrency configuration — all stages share `ConcurrentDownloads`.

#### Scenario: Multiple muxes run in parallel
- **WHEN** 3 downloads complete at roughly the same time
- **THEN** all 3 are muxed concurrently by the stream stage independently

#### Scenario: Mux parallelism respects configuration
- **WHEN** `FunkArr__ConcurrentDownloads=2` is configured
- **THEN** at most 2 mux operations run concurrently, with additional completions waiting via backpressure
