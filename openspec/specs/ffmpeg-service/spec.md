## Purpose

Encapsulates all FFmpeg and ffprobe process execution behind `IFfmpegService`, providing HLS download, subtitle probing, subtitle extraction, and video/subtitle remuxing to MKV. Uses `IFileService` for path resolution and centralizes process lifecycle management.

## Requirements

### Requirement: IFfmpegService interface
The system SHALL provide an `IFfmpegService` interface in the `FunkArr.DownloadClient` namespace that encapsulates all FFmpeg and ffprobe process execution. The implementation SHALL use `IFileService` for all path resolution and SHALL NOT accept directory paths as parameters.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `IFfmpegService` SHALL be registered as a singleton in the DI container with `FfmpegService` as the implementation

### Requirement: HLS video download
`IFfmpegService.DownloadHlsAsync(string nzoId, string url, CancellationToken ct)` SHALL invoke FFmpeg to download an HLS manifest URL to the temp video path resolved from `IFileService.GetTempVideoPath(nzoId)`. The method SHALL use stream-copy mode (`-c copy`) and map only video and audio streams.

#### Scenario: Successful HLS download
- **WHEN** `DownloadHlsAsync` is called with a valid HLS manifest URL
- **THEN** FFmpeg SHALL be invoked with arguments `-i "{url}" -map 0:v -map 0:a -c copy -y "{outputPath}"` and the method SHALL complete without throwing

#### Scenario: HLS download timeout
- **WHEN** the FFmpeg process does not exit within 30 minutes
- **THEN** the process SHALL be killed and the method SHALL throw an exception with a timeout message

#### Scenario: FFmpeg exits with non-zero code
- **WHEN** FFmpeg exits with a non-zero exit code during HLS download
- **THEN** the method SHALL throw an exception containing the exit code and stderr output

### Requirement: Subtitle stream probing
`IFfmpegService.HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct)` SHALL invoke ffprobe to check whether the given URL contains a subtitle stream.

#### Scenario: Stream with subtitle track
- **WHEN** `HasSubtitleStreamAsync` is called with a URL whose streams include `codec_type: "subtitle"`
- **THEN** the method SHALL return `true`

#### Scenario: Stream without subtitle track
- **WHEN** `HasSubtitleStreamAsync` is called with a URL whose streams contain no subtitle tracks
- **THEN** the method SHALL return `false`

#### Scenario: ffprobe failure
- **WHEN** ffprobe exits with a non-zero code or throws an exception
- **THEN** the method SHALL return `false` (fail-safe, treat as no subtitle)

### Requirement: Subtitle extraction from HLS
`IFfmpegService.ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct)` SHALL probe the manifest for subtitle streams and, if found, extract the first subtitle stream to SRT format at the path resolved from `IFileService.GetSubtitlePath(nzoId, ".srt")`.

#### Scenario: Subtitle extracted successfully
- **WHEN** the manifest contains a subtitle stream and FFmpeg extracts it
- **THEN** the method SHALL return `true`

#### Scenario: No subtitle stream in manifest
- **WHEN** `HasSubtitleStreamAsync` returns `false` for the manifest
- **THEN** the method SHALL return `false` without invoking FFmpeg for extraction

#### Scenario: Extraction fails
- **WHEN** FFmpeg fails to extract the subtitle (non-zero exit or output file missing)
- **THEN** the method SHALL return `false`

### Requirement: Video/subtitle remuxing to MKV
`IFfmpegService.RemuxAsync(string nzoId, string title, bool hasSubtitle, CancellationToken ct)` SHALL ensure the output directory exists via `IFileService`, build FFmpeg arguments for remuxing video (and optionally subtitle) into MKV format, execute FFmpeg, clean up temp files via `IFileService.CleanupTemp(nzoId)`, and return the output path.

#### Scenario: Remux with subtitle
- **WHEN** `RemuxAsync` is called with `hasSubtitle: true`
- **THEN** FFmpeg SHALL be invoked with both video and subtitle inputs, mapping video/audio/subtitle streams, with language metadata set to German, writing to the output path from `IFileService.GetOutputPath(title)`

#### Scenario: Remux without subtitle
- **WHEN** `RemuxAsync` is called with `hasSubtitle: false`
- **THEN** FFmpeg SHALL be invoked with only the video input, mapping video/audio streams, with language metadata set to German

#### Scenario: Temp files cleaned up after successful remux
- **WHEN** remux completes successfully
- **THEN** `IFileService.CleanupTemp(nzoId)` SHALL be called to remove the temp video and subtitle files

#### Scenario: Remux timeout
- **WHEN** the FFmpeg process does not exit within 600 seconds
- **THEN** the process SHALL be killed and the method SHALL throw an exception

### Requirement: Centralized process execution
The `FfmpegService` implementation SHALL use a shared internal method for process creation, execution, and error handling. This method SHALL handle process startup with redirected stdout/stderr, cancellation token propagation via timeout + kill, exit code evaluation, and process disposal.

#### Scenario: Process killed on cancellation
- **WHEN** the CancellationToken is cancelled while an FFmpeg process is running
- **THEN** the process SHALL be killed with `Kill(entireProcessTree: true)` and the method SHALL throw `OperationCanceledException`

#### Scenario: Process resources disposed
- **WHEN** an FFmpeg operation completes (success or failure)
- **THEN** the Process object SHALL be disposed

### Requirement: Argument building testability
FFmpeg argument construction methods SHALL be `internal static` on `FfmpegService` to allow unit testing of argument patterns without process execution.

#### Scenario: HLS args testable
- **WHEN** `BuildHlsDownloadArgs(url, outputPath)` is called
- **THEN** it SHALL return the expected FFmpeg argument string

#### Scenario: Remux args with subtitle testable
- **WHEN** `BuildRemuxArgs(videoPath, subtitlePath, outputPath)` is called with a non-null subtitlePath
- **THEN** it SHALL return arguments including both video and subtitle inputs with language metadata
