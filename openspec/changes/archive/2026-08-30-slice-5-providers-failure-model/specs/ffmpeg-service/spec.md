## RENAMED Requirements

### Requirement: FfmpegService renamed to FfmpegProvider
- **FROM:** `FfmpegService`/`IFfmpegService` in `DownloadClient/Ffmpeg/`
- **TO:** `FfmpegProvider`/`IFfmpegProvider` in `Providers/Ffmpeg/`

### Requirement: IFfmpegService renamed to IFfmpegProvider
- **FROM:** `IFfmpegService` interface
- **TO:** `IFfmpegProvider` interface

## MODIFIED Requirements

### Requirement: IFfmpegProvider interface
The system SHALL provide an `IFfmpegProvider` interface (renamed from `IFfmpegService`) in the `Providers/Ffmpeg/` folder that encapsulates all FFmpeg and ffprobe process execution. The implementation SHALL use `IFileSystemProvider` for all path resolution.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `IFfmpegProvider` SHALL be registered as a singleton in the DI container with `FfmpegProvider` as the implementation
