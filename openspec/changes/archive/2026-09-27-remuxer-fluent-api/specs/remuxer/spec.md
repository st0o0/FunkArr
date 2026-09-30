## MODIFIED Requirements

### Requirement: Remuxer orchestrates subtitle preparation and FFmpeg execution
The Remuxer SHALL accept a `RemuxOptions` record, resolve the route from `Channel` via `IRouteResolver`, delegate subtitle download to `ISubtitleDownloader` and FFmpeg execution to `IFfmpegProcess`.

#### Scenario: Download with subtitle URL
- **WHEN** `RunAsync` is called with a RemuxOptions that has a non-null SubtitleUrl
- **THEN** the Remuxer SHALL resolve the route from Channel, call ISubtitleDownloader with the subtitle URL and resolved route
- **AND** on success, build FfmpegInput with the local subtitle path and call IFfmpegProcess

#### Scenario: Download without subtitle URL
- **WHEN** `RunAsync` is called with a RemuxOptions that has null SubtitleUrl
- **THEN** the Remuxer SHALL resolve the route and call IFfmpegProcess without subtitle

#### Scenario: Subtitle preparation fails
- **WHEN** ISubtitleDownloader returns SubtitleResult.Failed
- **THEN** the Remuxer SHALL log the failure and proceed without subtitles

#### Scenario: Channel is null
- **WHEN** RemuxOptions.Channel is null
- **THEN** the Remuxer SHALL use the default route (no proxy)

### Requirement: Remuxer interface accepts RemuxOptions
The `IRemuxer` interface SHALL accept `RemuxOptions` instead of individual parameters.

#### Scenario: Interface signature
- **WHEN** a consumer needs to remux media
- **THEN** the interface SHALL expose `Task<FfmpegResult> RunAsync(RemuxOptions options, Action<ProgressUpdate> onProgress, CancellationToken ct)`

### Requirement: Remuxer composes ISubtitleDownloader and IFfmpegProcess via DI
The `Remuxer` SHALL receive `IRouteResolver`, `ISubtitleDownloader`, `IFfmpegProcess`, and `ILogger<Remuxer>` through constructor injection.

#### Scenario: DI registration
- **WHEN** the Remuxer is registered in DI
- **THEN** it SHALL be registered as `IRemuxer` with its internal dependencies resolved from the container

### Requirement: Remuxer cleans up temp subtitle files
The Remuxer SHALL delete the temporary subtitle file after FFmpeg completes, regardless of success or failure.

#### Scenario: Cleanup after completion
- **WHEN** FFmpeg completes and a temp subtitle file was created
- **THEN** the Remuxer SHALL delete the temp subtitle file
