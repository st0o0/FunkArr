## MODIFIED Requirements

### Requirement: Stage machine flow
The DownloadCoordinator SHALL progress through stages: `Accepted → Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done`. Each stage spawns a transient child worker. The `StartDownload` message SHALL include an optional `Category` field alongside `NzoId`, `VideoUrl`, `SubtitleUrl`, and `Title`.

#### Scenario: Full download pipeline with category
- **WHEN** `StartDownload` is received with a direct MP4 URL, subtitle URL, and category `"tv"`
- **THEN** the coordinator SHALL store category, progress through all stages, and pass category to `RemuxVideo` so `FfmpegService` can resolve the output path via `FileService`

#### Scenario: No subtitle available
- **WHEN** no subtitle URL is provided and the source is not HLS
- **THEN** the coordinator SHALL skip AcquiringSubtitle and ConvertingSubtitle, proceeding directly to Muxing

### Requirement: Category threading to workers
The DownloadCoordinator SHALL pass category to workers that need output path resolution. Specifically, `RemuxVideo` SHALL include the category so `FfmpegService` can call `FileService.GetOutputPath(title, category)` and `FileService.EnsureOutputDirectory(title, category)`.

#### Scenario: Category passed to RemuxWorker
- **WHEN** the coordinator enters the Muxing stage with category `"tv"`
- **THEN** `RemuxVideo` SHALL include `category: "tv"` alongside nzoId, title, and hasSubtitle

### Requirement: Status updates to DownloadRequestTracker
On each stage transition, the DownloadCoordinator SHALL tell the DownloadRequestTracker shard with `UpdateStatus`. On completion, it SHALL tell `MarkCompleted`. On failure, it SHALL tell `MarkFailed`.

#### Scenario: Status forwarded on stage change
- **WHEN** the coordinator enters the Muxing stage
- **THEN** it SHALL tell DownloadRequestTracker with `UpdateStatus(nzoId, "Muxing")`
