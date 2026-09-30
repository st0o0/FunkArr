## MODIFIED Requirements

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

### Requirement: Remuxer composes ISubtitlePreparer and IFfmpegRunner via DI
The `Remuxer` SHALL receive `IHttpClientFactory`, `ILogger<Remuxer>`, and `TimeProvider` through constructor injection. It SHALL NOT depend on `IFfmpegRunner` or `ISubtitlePreparer`.

#### Scenario: DI registration
- **WHEN** the Remuxer is registered in DI
- **THEN** it SHALL be registered as `IRemuxer` with `IHttpClientFactory` and `ILogger<Remuxer>` resolved from the container
- **AND** `IFfmpegRunner` and `ISubtitlePreparer` SHALL NOT be registered

### Requirement: Remuxer interface
The `IRemuxer` interface SHALL keep its current method signature unchanged.

#### Scenario: Interface signature
- **WHEN** a consumer needs to remux media
- **THEN** the interface SHALL expose `Task<FfmpegResult> RunAsync(string videoUrl, string? subtitleUrl, string outputPath, string routeName, string? proxyUrl, Action<ProgressUpdate> onProgress, CancellationToken ct)`
