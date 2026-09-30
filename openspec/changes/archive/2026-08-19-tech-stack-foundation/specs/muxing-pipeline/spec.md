## ADDED Requirements

### Requirement: Video remuxing to MKV
The MuxingActor SHALL remux downloaded video files into MKV containers using FFmpeg with stream-copy (no re-encoding). The output MUST have correct language metadata set to German.

#### Scenario: MP4 to MKV remux
- **WHEN** a downloaded MP4 file is passed to the MuxingActor
- **THEN** FFmpeg runs with `-c copy` flags, produces an MKV file with `language=ger` metadata on video and audio streams, and the original MP4 is deleted

### Requirement: Subtitle merging
The MuxingActor SHALL merge subtitle files into the MKV container during remuxing when subtitles are available.

#### Scenario: Video with SRT subtitle
- **WHEN** a video file and an SRT subtitle file are available for the same content
- **THEN** FFmpeg maps both into the output MKV with the subtitle stream tagged as `language=ger`

#### Scenario: Video without subtitles
- **WHEN** only a video file is available (no subtitle file)
- **THEN** FFmpeg remuxes the video alone into MKV without subtitle streams

### Requirement: Subtitle format normalization
The system SHALL convert non-SRT subtitle formats (VTT, TTML) to SRT before muxing.

#### Scenario: VTT subtitle conversion
- **WHEN** a subtitle file in WebVTT format is downloaded
- **THEN** the system converts it to SRT format before passing to the MuxingActor

### Requirement: FFmpeg process management
The MuxingActor SHALL manage FFmpeg as an external process with timeout protection and error detection.

#### Scenario: FFmpeg completes successfully
- **WHEN** FFmpeg exits with code 0
- **THEN** the MuxingActor reports success and the download job moves to "Completed" status

#### Scenario: FFmpeg fails
- **WHEN** FFmpeg exits with a non-zero exit code
- **THEN** the MuxingActor reports failure, the error output is logged, and the download job is marked as "Failed"

#### Scenario: FFmpeg hangs
- **WHEN** FFmpeg does not complete within the configured timeout (default 10 minutes)
- **THEN** the process is killed and the job is marked as "Failed"

### Requirement: Temp file cleanup
The system SHALL clean up temporary files (downloaded source video, intermediate subtitle files) after successful muxing.

#### Scenario: Cleanup after successful mux
- **WHEN** muxing completes successfully
- **THEN** the source MP4 and any intermediate subtitle files are deleted, leaving only the final MKV

#### Scenario: Cleanup after failed mux
- **WHEN** muxing fails
- **THEN** temporary files are preserved for debugging and the job is marked as "Failed"
