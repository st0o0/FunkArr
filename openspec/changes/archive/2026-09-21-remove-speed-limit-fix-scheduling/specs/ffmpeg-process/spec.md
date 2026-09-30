## MODIFIED Requirements

### Requirement: FFmpeg runner as injectable service
The system SHALL provide `IFfmpegRunner` as a DI-injectable interface with no Akka dependency, registered via an extension method on `IServiceCollection`. The `RunAsync` method SHALL accept `videoUrl`, `subtitlePath`, `outputPath`, `onProgress` callback, and `CancellationToken`. It SHALL NOT accept a speed limit parameter.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** the host SHALL call `AddFfmpegRunner()` extension method, which registers `IFfmpegRunner` as a singleton

#### Scenario: Worker injection
- **WHEN** a DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner` via constructor injection and use it to start FFmpeg processes

#### Scenario: Type visibility
- **WHEN** the FunkArr.Download project is referenced by other projects
- **THEN** `FfmpegRunner` (implementation) SHALL be internal; `IFfmpegRunner`, `FfmpegResult`, and `ProgressUpdate` SHALL be public (required by the public DownloadWorker constructor)

#### Scenario: No speed limit parameter
- **WHEN** `IFfmpegRunner.RunAsync()` is called
- **THEN** the method signature SHALL NOT include a `speedLimitBytesPerSecond` parameter
- **AND** no `-maxrate` or `-bufsize` arguments SHALL be passed to FFmpeg
