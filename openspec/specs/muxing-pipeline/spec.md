## Purpose

FFmpeg process management for remuxing downloaded video + subtitle streams into MKV containers with correct German language metadata. Exposed as a stateless MuxingService for use within the Akka.Streams download pipeline.

## Requirements

### Requirement: Video remuxing to MKV
The MuxingService SHALL remux downloaded video files into MKV containers using FFmpeg with stream-copy (no re-encoding). The output MUST have correct language metadata set to German. MuxingService is a stateless DI-registered service, not an actor.

#### Scenario: MP4 to MKV remux
- **WHEN** a downloaded MP4 file is passed to `MuxingService.MuxAsync()`
- **THEN** FFmpeg runs with `-c copy` flags, produces an MKV file with `language=ger` metadata on video and audio streams, and the original MP4 is deleted

#### Scenario: Muxing called from stream stage
- **WHEN** the mux stage of the download stream pipeline receives a `DownloadOutcome.Success`
- **THEN** it calls `MuxingService.MuxAsync()` with the video path, subtitle path, output directory, and title, returning a `MuxOutcome`

### Requirement: Subtitle merging
The MuxingService SHALL merge subtitle files into the MKV container during remuxing when subtitles are available.

#### Scenario: Video with SRT subtitle
- **WHEN** a video file and an SRT subtitle file are available for the same content
- **THEN** FFmpeg maps both into the output MKV with the subtitle stream tagged as `language=ger`

#### Scenario: Video without subtitles
- **WHEN** only a video file is available (no subtitle file)
- **THEN** FFmpeg remuxes the video alone into MKV without subtitle streams

### Requirement: Subtitle format normalization
The MuxingService SHALL convert non-SRT subtitle formats (VTT, TTML) to SRT before muxing.

#### Scenario: VTT subtitle conversion
- **WHEN** a subtitle file in WebVTT format is downloaded
- **THEN** the MuxingService converts it to SRT format before passing to FFmpeg

### Requirement: FFmpeg process management
The MuxingService SHALL manage FFmpeg as an external process with timeout protection and error detection.

#### Scenario: FFmpeg completes successfully
- **WHEN** FFmpeg exits with code 0
- **THEN** `MuxAsync` returns a `MuxOutcome.Success` with the output file path

#### Scenario: FFmpeg fails
- **WHEN** FFmpeg exits with a non-zero exit code
- **THEN** `MuxAsync` returns a `MuxOutcome.Failure` with the error details

#### Scenario: FFmpeg hangs
- **WHEN** FFmpeg does not complete within the configured timeout (default 10 minutes)
- **THEN** the process is killed and `MuxAsync` returns a `MuxOutcome.Failure`

### Requirement: Temp file cleanup
The MuxingService SHALL clean up temporary files (downloaded source video, intermediate subtitle files) after successful muxing.

#### Scenario: Cleanup after successful mux
- **WHEN** muxing completes successfully
- **THEN** the source MP4 and any intermediate subtitle files are deleted, leaving only the final MKV

#### Scenario: Cleanup after failed mux
- **WHEN** muxing fails
- **THEN** temporary files are preserved for debugging

### Requirement: Parallel mux execution
The download pipeline SHALL run multiple mux operations concurrently up to a configurable limit (default 4) via the stream's `SelectAsyncUnordered` stage.

#### Scenario: Multiple muxes run in parallel
- **WHEN** 4 downloads complete at roughly the same time
- **THEN** all 4 are muxed concurrently by the stream stage, each calling `MuxingService.MuxAsync()` independently

#### Scenario: Mux parallelism respects configuration
- **WHEN** `FunkArr__MuxConcurrency=2` is configured
- **THEN** at most 2 mux operations run concurrently, with additional completions waiting via backpressure
