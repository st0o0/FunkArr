# Remuxer

## Purpose

Orchestrates the full media remux pipeline: subtitle preparation, FFmpeg execution with local inputs, and temp file cleanup. Replaces `IFfmpegRunner` as the `DownloadWorker`'s dependency.

## Requirements

### Requirement: Remuxer orchestrates subtitle preparation and FFmpeg execution
The Remuxer SHALL coordinate `ISubtitlePreparer` and `IFfmpegRunner` to produce a remuxed MKV file with optional subtitles. The Remuxer SHALL handle `SubtitleResult` instead of bare `string?` from the preparer.

#### Scenario: Download with subtitle URL
- **WHEN** `RunAsync` is called with a non-null subtitle URL
- **THEN** the Remuxer SHALL call `ISubtitlePreparer.PrepareAsync` to obtain a `SubtitleResult`
- **AND** on `SubtitleResult.Success`, call `IFfmpegRunner.RunAsync` with the local subtitle path and language from the track
- **AND** return the `FfmpegResult` from the runner

#### Scenario: Download without subtitle URL
- **WHEN** `RunAsync` is called with a null subtitle URL
- **THEN** the Remuxer SHALL call `IFfmpegRunner.RunAsync` with null subtitle path
- **AND** skip subtitle preparation entirely

#### Scenario: Subtitle preparation fails
- **WHEN** `ISubtitlePreparer.PrepareAsync` returns `SubtitleResult.Failed`
- **THEN** the Remuxer SHALL log the failure reason at warning level
- **AND** call `IFfmpegRunner.RunAsync` with null subtitle path
- **AND** the download SHALL proceed without subtitles

#### Scenario: Subtitle unavailable
- **WHEN** `ISubtitlePreparer.PrepareAsync` returns `SubtitleResult.Unavailable`
- **THEN** the Remuxer SHALL call `IFfmpegRunner.RunAsync` with null subtitle path

#### Scenario: Language passed to FFmpeg
- **WHEN** subtitle preparation succeeds with a `SubtitleTrack` containing `Language`
- **THEN** the Remuxer SHALL pass the language value to `IFfmpegRunner.RunAsync`

### Requirement: Remuxer cleans up temp subtitle files
The Remuxer SHALL delete the temporary subtitle file after FFmpeg completes, regardless of success or failure.

#### Scenario: Cleanup after successful download
- **WHEN** FFmpeg completes successfully and a temp subtitle file was created
- **THEN** the Remuxer SHALL delete the temp subtitle file

#### Scenario: Cleanup after failed download
- **WHEN** FFmpeg fails and a temp subtitle file was created
- **THEN** the Remuxer SHALL delete the temp subtitle file

#### Scenario: No cleanup when no subtitle
- **WHEN** no temp subtitle file was created (subtitle was null, failed, or unavailable)
- **THEN** the Remuxer SHALL not attempt any file cleanup

### Requirement: Remuxer forwards progress updates
The Remuxer SHALL forward all progress updates from `IFfmpegRunner` to the caller without modification.

#### Scenario: Progress passthrough
- **WHEN** `IFfmpegRunner` reports a `ProgressUpdate`
- **THEN** the Remuxer SHALL invoke the caller's `onProgress` callback with the same update

### Requirement: Remuxer supports cancellation
The Remuxer SHALL observe the provided `CancellationToken` and propagate cancellation to both subtitle preparation and FFmpeg execution.

#### Scenario: Cancellation during subtitle preparation
- **WHEN** the CancellationToken is cancelled during subtitle preparation
- **THEN** the Remuxer SHALL cancel the preparation and return a cancelled result

#### Scenario: Cancellation during FFmpeg execution
- **WHEN** the CancellationToken is cancelled during FFmpeg execution
- **THEN** the Remuxer SHALL propagate cancellation to `IFfmpegRunner`

### Requirement: Remuxer interface
The `IRemuxer` interface SHALL match the current method signature with no changes.

#### Scenario: Interface signature
- **WHEN** a consumer needs to remux media
- **THEN** the interface SHALL expose `Task<FfmpegResult> RunAsync(string videoUrl, string? subtitleUrl, string outputPath, string routeName, string? proxyUrl, Action<ProgressUpdate> onProgress, CancellationToken ct)`

### Requirement: Remuxer composes ISubtitlePreparer and IFfmpegRunner via DI
The `Remuxer` SHALL receive `ISubtitlePreparer` and `IFfmpegRunner` through constructor injection.

#### Scenario: DI registration
- **WHEN** the Remuxer is registered in DI
- **THEN** it SHALL be registered as `IRemuxer` with its dependencies resolved from the container
