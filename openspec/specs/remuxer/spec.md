# Remuxer

## Purpose

Orchestrates the full media remux pipeline: subtitle preparation, FFmpeg execution with local inputs, and temp file cleanup. Replaces `IFfmpegRunner` as the `DownloadWorker`'s dependency.

## Requirements

### Requirement: Remuxer orchestrates subtitle preparation and FFmpeg execution
The Remuxer SHALL handle subtitle download/preparation and FFmpeg execution internally, without delegating to separate ISubtitlePreparer or IFfmpegRunner interfaces. It SHALL build a `RemuxOptions` record and pass it to an internal `BuildArguments` method.

#### Scenario: Download with subtitle URL
- **WHEN** `RunAsync` is called with a non-null subtitle URL
- **THEN** the Remuxer SHALL download and convert the subtitle internally
- **AND** build a `RemuxOptions` with the local subtitle path and detected language
- **AND** execute FFmpeg with the built arguments

#### Scenario: Download without subtitle URL
- **WHEN** `RunAsync` is called with a null subtitle URL
- **THEN** the Remuxer SHALL build a `RemuxOptions` with null subtitle path
- **AND** execute FFmpeg without subtitle arguments

#### Scenario: Subtitle preparation fails
- **WHEN** subtitle download or conversion fails
- **THEN** the Remuxer SHALL log the failure at warning level
- **AND** proceed with the download without subtitles

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
The `IRemuxer` interface SHALL keep its current method signature unchanged.

#### Scenario: Interface signature
- **WHEN** a consumer needs to remux media
- **THEN** the interface SHALL expose `Task<FfmpegResult> RunAsync(string videoUrl, string? subtitleUrl, string outputPath, string routeName, string? proxyUrl, Action<ProgressUpdate> onProgress, CancellationToken ct)`

### Requirement: Remuxer composes ISubtitlePreparer and IFfmpegRunner via DI
The `Remuxer` SHALL receive `IHttpClientFactory`, `ILogger<Remuxer>`, and `TimeProvider` through constructor injection. It SHALL NOT depend on `IFfmpegRunner` or `ISubtitlePreparer`.

#### Scenario: DI registration
- **WHEN** the Remuxer is registered in DI
- **THEN** it SHALL be registered as `IRemuxer` with `IHttpClientFactory` and `ILogger<Remuxer>` resolved from the container
- **AND** `IFfmpegRunner` and `ISubtitlePreparer` SHALL NOT be registered
