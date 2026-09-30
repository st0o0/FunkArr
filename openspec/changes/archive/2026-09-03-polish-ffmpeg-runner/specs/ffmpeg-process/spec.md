# FFmpeg Process

## MODIFIED Requirements

### Requirement: FFmpeg process management via FFMpegCore
The system SHALL use FFMpegCore's `ProcessAsynchronously` with `throwOnError: true` to manage the FFmpeg process lifecycle, catching `FFMpegException` for error details.

#### Scenario: Process exit success
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the runner SHALL return an `FfmpegResult` with `Success=true`, `ExitCode=0`, `Error=null`, and the elapsed time in seconds

#### Scenario: Process exit failure
- **WHEN** the FFmpeg process exits with a non-zero code
- **THEN** the runner SHALL catch the `FFMpegException`, extract `FFMpegErrorOutput` as stderr, cap it to the last 4096 characters, and return an `FfmpegResult` with `Success=false`, `ExitCode=1`, the capped stderr as `Error`, and the elapsed time in seconds

#### Scenario: Cancellation
- **WHEN** the CancellationToken is cancelled during a running download
- **THEN** the runner SHALL terminate the FFmpeg process and return an `FfmpegResult` with `Success=false`, `ExitCode=-1`, and `Error="Cancelled"`

### Requirement: FFmpeg runner as injectable service
The system SHALL provide `IFfmpegRunner` as a DI-injectable interface with no Akka dependency, registered via an extension method on `IServiceCollection`.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** the host SHALL call `AddFfmpegRunner()` extension method, which registers `IFfmpegRunner` as a singleton

#### Scenario: Worker injection
- **WHEN** a DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner` via constructor injection and use it to start FFmpeg processes

#### Scenario: Type visibility
- **WHEN** the FunkArr.Download project is referenced by other projects
- **THEN** `IFfmpegRunner`, `FfmpegRunner`, `FfmpegResult`, and `ProgressUpdate` SHALL be internal to the Download domain
