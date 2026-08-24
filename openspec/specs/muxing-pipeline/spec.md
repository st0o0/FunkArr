## Purpose

FFmpeg process management for remuxing downloaded video + subtitle streams into MKV containers with correct German language metadata. The `RemuxActor` is a transient child actor of `DownloadActor`; the actual FFmpeg invocation is delegated to `FfmpegService.RemuxAsync`.

## Requirements

### Requirement: Video remuxing to MKV via RemuxActor
The `RemuxActor` SHALL be a transient child `ReceiveActor` of `DownloadActor` in namespace `FunkArr.DownloadClient.Pipeline`. It SHALL delegate remuxing to `IFfmpegService.RemuxAsync(nzoId, title, hasSubtitle, category)` and report results to its parent. The actor records mux duration via `FunkArrMetrics.Instance.AddMuxDuration()` histogram with an `outcome` tag (`"success"` or `"error"`).

#### Scenario: Successful remux
- **WHEN** the actor receives a `RemuxVideo(nzoId, title, hasSubtitle, category)` command
- **THEN** it calls `FfmpegService.RemuxAsync`, tells the parent `VideoRemuxed(nzoId)` on success, records duration with outcome `"success"`, and stops itself

#### Scenario: Remux fails
- **WHEN** `RemuxAsync` throws an exception
- **THEN** the actor tells the parent `WorkerFailed(nzoId, FailureKind.Malformed, ex.Message)`, records duration with outcome `"error"`, and stops itself

### Requirement: FFmpeg remux logic in FfmpegService
`FfmpegService.RemuxAsync` in namespace `FunkArr.DownloadClient.Ffmpeg` SHALL remux downloaded video files into MKV containers using FFmpeg with stream-copy (no re-encoding). The output MUST have correct language metadata set to German (`language=ger` on video, audio, and subtitle streams).

#### Scenario: MP4 to MKV remux without subtitles
- **WHEN** `RemuxAsync` is called with `hasSubtitle: false`
- **THEN** FFmpeg runs with `-i video -map 0:v -map 0:a -c copy -metadata:s:v:0 language=ger -metadata:s:a:0 language=ger -y output.mkv`

#### Scenario: MP4 to MKV remux with subtitles
- **WHEN** `RemuxAsync` is called with `hasSubtitle: true`
- **THEN** FFmpeg maps both video and subtitle inputs with `-i video -i subtitle -map 0:v -map 0:a -map 1:s -c copy -c:s srt` and tags all streams (video, audio, subtitle) with `language=ger`

### Requirement: Output directory and file management
`FfmpegService.RemuxAsync` SHALL use `IFileService` to resolve paths: `GetTempVideoPath(nzoId)` for input video, `GetNormalizedSubtitlePath(nzoId)` for input subtitle, `EnsureOutputDirectory(title, category)` to create the output folder, and `GetOutputPath(title, category)` for the final MKV path.

#### Scenario: Category-aware output path
- **WHEN** `RemuxAsync` is called with `category: "tv"`
- **THEN** `IFileService.EnsureOutputDirectory(title, "tv")` and `GetOutputPath(title, "tv")` are called to place the MKV in a category-specific directory

### Requirement: FFmpeg process management
`FfmpegService` SHALL manage FFmpeg as an external process with timeout protection (default 600 seconds for remux). On timeout, the process is killed via `Process.Kill(entireProcessTree: true)`.

#### Scenario: FFmpeg completes successfully
- **WHEN** FFmpeg exits with code 0
- **THEN** temp files are cleaned up via `IFileService.CleanupTemp(nzoId)` and the output path is returned

#### Scenario: FFmpeg fails
- **WHEN** FFmpeg exits with a non-zero exit code
- **THEN** `FfmpegService` throws `InvalidOperationException` with the FFmpeg stderr content

#### Scenario: FFmpeg hangs
- **WHEN** FFmpeg does not complete within the configured timeout
- **THEN** the process is killed and an `OperationCanceledException` is thrown

### Requirement: Transient child actor lifecycle
The `RemuxActor` SHALL stop itself after completing its work (success or failure) via `Context.Stop(Self)` in a `finally` block.

#### Scenario: Actor self-terminates after work
- **WHEN** remuxing completes (success or failure)
- **THEN** the actor stops itself, freeing resources in the parent's child collection
